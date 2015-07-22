using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.SQS.Model;
using ServiceStack.Text;

namespace ServiceStack.Aws.Sqs.Fake
{
    public class FakeSqsQueue
    {
        public const string FakeBatchItemFailString = "ReturnFailureForThisBatchItem";

        private readonly ConcurrentDictionary<string, FakeSqsQueueItem> _inFlighItems = new ConcurrentDictionary<string, FakeSqsQueueItem>();
        private readonly ConcurrentQueue<FakeSqsQueueItem> _qItems = new ConcurrentQueue<FakeSqsQueueItem>();
        private long _lastInFlightRequeue = DateTime.UtcNow.ToUnixTime();

        public SqsQueueDefinition QueueDefinition { get; set; }

        private FakeSqsQueueItem GetInFlightItem(string receiptHandle)
        {
            FakeSqsQueueItem item;

            // Certainly plenty of room here for race-conditions in terms of multiple threads getting the same
            // item and dealing with it, but since this is a fake service for testing, I'm not handling those
            // kinds of things at the moment...

            if (!_inFlighItems.TryGetValue(receiptHandle, out item))
            {
                throw new ReceiptHandleIsInvalidException("Handle [{0}] does not exist".Fmt(receiptHandle));
            }

            var status = item.GetStatus();

            if (status == FakeSqsItemStatus.InFlight)
            {
                return item;
            }

            if (status == FakeSqsItemStatus.Queued)
            {
                RequeueInFlightMessage(receiptHandle);
            }

            throw new MessageNotInflightException("Item with handle [{0}] is not in flight".Fmt(receiptHandle));
        }

        public bool ChangeVisibility(ChangeMessageVisibilityRequest request)
        {
            if (request.ReceiptHandle.Equals(FakeBatchItemFailString, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            var item = GetInFlightItem(request.ReceiptHandle);

            if (request.VisibilityTimeout <= 0)
            {
                RequeueInFlightMessage(request.ReceiptHandle, force: true);
            }
            else
            {
                item.InFlightUntil = item.InFlightUntil += request.VisibilityTimeout;
            }

            return true;
        }

        public bool DeleteMessage(DeleteMessageRequest request)
        {   // Handle still has to be valid
            if (request.ReceiptHandle.Equals(FakeBatchItemFailString, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            var item = GetInFlightItem(request.ReceiptHandle);
            return _inFlighItems.TryRemove(request.ReceiptHandle, out item);
        }

        private void RequeueInFlightMessage(string receiptHandle, bool force = false)
        {
            FakeSqsQueueItem item;

            if (!_inFlighItems.TryRemove(receiptHandle, out item))
            {
                return;
            }

            var status = item.GetStatus();

            if (force || status == FakeSqsItemStatus.Queued)
            {
                item.InFlightUntil = 0;
                item.Status = FakeSqsItemStatus.Queued;

                _qItems.Enqueue(item);
            }
            else if (status == FakeSqsItemStatus.InFlight)
            {
                _inFlighItems.TryAdd(receiptHandle, item);
            }
        }

        private void RequeueExpiredInFlightMessages()
        {
            // Inefficient at the moment, but works for testing...

            //var now = DateTime.UtcNow.ToUnixTime();

            //if ((now - _lastInFlightRequeue) < 0)
            //{
            //    return;
            //}

            //_lastInFlightRequeue = now;

            var requeueItems = _inFlighItems.Where(inf => inf.Value.GetStatus() == FakeSqsItemStatus.Queued)
                                            .Select(inf => inf.Key);

            foreach (var fKey in requeueItems)
            {
                RequeueInFlightMessage(fKey);
            }
        }

        public IEnumerable<FakeSqsQueueItem> Receive(ReceiveMessageRequest request)
        {
            RequeueExpiredInFlightMessages();

            var count = 0;
            
            var visibilityTimeout = request.VisibilityTimeout <= 0
                                        ? this.QueueDefinition.VisibilityTimeout
                                        : SqsQueueDefinition.GetValidVisibilityTimeout(request.VisibilityTimeout);

            var foundItem = false;

            var timeoutAt = DateTime.UtcNow.AddSeconds(request.WaitTimeSeconds).ToUnixTime();
            
            do
            {
                FakeSqsQueueItem qi;

                foundItem = _qItems.TryDequeue(out qi);

                if (!foundItem)
                {
                    Thread.Sleep(100);
                    continue;
                }

                qi.InFlightUntil = DateTime.UtcNow.AddSeconds(visibilityTimeout).ToUnixTime();
                qi.Status = FakeSqsItemStatus.InFlight;

                if (!_inFlighItems.TryAdd(qi.ReceiptHandle, qi))
                {
                    throw new ReceiptHandleIsInvalidException("Could not flight queued item with handle [{0}]".Fmt(qi.ReceiptHandle));
                }

                count++;
                yield return qi;

            } while (count < request.MaxNumberOfMessages &&
                     (foundItem || timeoutAt >= DateTime.UtcNow.ToUnixTime()));

        }

        public string Send(SendMessageRequest request)
        {
            if (request.MessageBody.Equals(FakeBatchItemFailString, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            var newItem = new FakeSqsQueueItem
                          {
                              Body = request.MessageBody,
                              Status = FakeSqsItemStatus.Queued
                          };

            _qItems.Enqueue(newItem);

            return newItem.MessageId;
        }

        public Int32 Count
        {
            get { return _qItems.Count + _inFlighItems.Count; }
        }

        public void Clear()
        {
            while (_qItems.Count > 0)
            {
                FakeSqsQueueItem qi;

                while (_qItems.TryDequeue(out qi))
                {
                    continue;
                }
            }
        }

    }
}