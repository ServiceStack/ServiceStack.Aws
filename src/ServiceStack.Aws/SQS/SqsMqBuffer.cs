using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Amazon.SQS;
using Amazon.SQS.Model;
using ServiceStack.Aws.Interfaces;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.SQS
{
    public class SqsMqBuffer : ISqsMqBuffer
    {
        private readonly SqsQueueDefinition _queueDefinition;
        private readonly SqsConnectionFactory _sqsConnectionFactory;
        private IAmazonSQS _sqsClient;

        private readonly ConcurrentQueue<Message> _receiveBuffer = new ConcurrentQueue<Message>();
        private readonly ConcurrentQueue<DeleteMessageRequest> _deleteBuffer = new ConcurrentQueue<DeleteMessageRequest>();
        private readonly ConcurrentQueue<SendMessageRequest> _sendBuffer = new ConcurrentQueue<SendMessageRequest>();
        private readonly ConcurrentQueue<ChangeMessageVisibilityRequest> _cvBuffer = new ConcurrentQueue<ChangeMessageVisibilityRequest>();
        
        public SqsMqBuffer(SqsQueueDefinition queueDefinition,
                           SqsConnectionFactory sqsConnectionFactory)
        {
            Guard.AgainstNullArgument(queueDefinition, "queueDefinition");
            Guard.AgainstNullArgument(sqsConnectionFactory, "sqsConnectionFactory");

            _queueDefinition = queueDefinition;
            _sqsConnectionFactory = sqsConnectionFactory;
        }

        private IAmazonSQS SqsClient
        {
            get { return _sqsClient ?? (_sqsClient = _sqsConnectionFactory.GetClient()); }
        }

        public SqsQueueDefinition QueueDefinition
        {
            get { return _queueDefinition; }
        }

        public Action<Exception> ErrorHandler { get; set; }

        private void HandleError(Exception ex)
        {
            if (ErrorHandler != null && ex != null)
            {
                ErrorHandler(ex);
            }
        }

        public bool ChangeVisibility(ChangeMessageVisibilityRequest request)
        {
            if (request == null)
            {
                return false;
            }

            _cvBuffer.Enqueue(request);
            return CvEnqueued(_queueDefinition.ChangeVisibilityBufferSize);
        }

        private bool CvEnqueued(int minBufferCount, bool forceOne = false)
        {
            var cvAtAws = false;
            minBufferCount = Math.Min(SqsQueueDefinition.MaxBatchCvItems, Math.Max(minBufferCount, 1));

            try
            {
                while (forceOne || _cvBuffer.Count >= minBufferCount)
                {
                    forceOne = false;

                    var entries = EntriesToCv(SqsQueueDefinition.MaxBatchCvItems).ToList();

                    if (entries.Count <= 0)
                    {
                        break;
                    }

                    cvAtAws = true;

                    var response = SqsClient.ChangeMessageVisibilityBatch(new ChangeMessageVisibilityBatchRequest
                                                                          {
                                                                              QueueUrl = _queueDefinition.QueueUrl,
                                                                              Entries = entries
                                                                          });

                    if (response.Failed != null && response.Failed.Count > 0)
                    {
                        response.Failed.Each(f => HandleError(f.ToException()));
                    }
                }
            }
            catch(Exception ex)
            {
                HandleError(ex);
            }

            return cvAtAws;
        }
        
        private IEnumerable<ChangeMessageVisibilityBatchRequestEntry> EntriesToCv(int count)
        {
            var result = new Dictionary<String, ChangeMessageVisibilityBatchRequestEntry>(count);

            while (result.Count < count)
            {
                ChangeMessageVisibilityRequest request;

                if (!_cvBuffer.TryDequeue(out request))
                {
                    return result.Values;
                }

                var id = request.ReceiptHandle.ToSha256HashString64();

                if (!result.ContainsKey(id))
                {
                    result.Add(id, new ChangeMessageVisibilityBatchRequestEntry
                                   {
                                       Id = id,
                                       ReceiptHandle = request.ReceiptHandle
                                   });
                }
            }

            return result.Values;
        }

        public bool Send(SendMessageRequest request)
        {
            if (request == null)
            {
                return false;
            }

            _sendBuffer.Enqueue(request);
            return SendEnqueued(_queueDefinition.SendBufferSize);
        }

        private bool SendEnqueued(int minBufferCount, bool forceOne = false)
        {
            var sentToSqs = false;
            minBufferCount = Math.Min(SqsQueueDefinition.MaxBatchSendItems, Math.Max(minBufferCount, 1));

            try
            {
                while (forceOne || _sendBuffer.Count >= minBufferCount)
                {
                    forceOne = false;

                    var entries = EntriesToSend(SqsQueueDefinition.MaxBatchSendItems).ToList();

                    if (entries.Count <= 0)
                    {
                        break;
                    }

                    sentToSqs = true;

                    var response = SqsClient.SendMessageBatch(new SendMessageBatchRequest
                                                              {
                                                                  QueueUrl = _queueDefinition.QueueUrl,
                                                                  Entries = entries
                                                              });

                    if (response.Failed != null && response.Failed.Count > 0)
                    {
                        response.Failed.Each(f => HandleError(f.ToException()));
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }

            return sentToSqs;
        }

        private IEnumerable<SendMessageBatchRequestEntry> EntriesToSend(int count)
        {
            var results = 0;

            while (results < count)
            {
                SendMessageRequest request;

                if (!_sendBuffer.TryDequeue(out request))
                {
                    yield break;
                }

                yield return new SendMessageBatchRequestEntry
                             {
                                 Id = Guid.NewGuid().ToString("N"),
                                 MessageBody = request.MessageBody,
                                 DelaySeconds = request.DelaySeconds,
                                 MessageAttributes = request.MessageAttributes
                             };

                results++;
            }
        }

        public bool Delete(DeleteMessageRequest request)
        {
            if (request == null)
            {
                return false;
            }

            _deleteBuffer.Enqueue(request);
            return DeleteEnqueued(_queueDefinition.DeleteBufferSize);
        }

        private bool DeleteEnqueued(int minBufferCount, bool forceOne = false)
        {
            var deletedAtSqs = false;
            minBufferCount = Math.Min(SqsQueueDefinition.MaxBatchDeleteItems, Math.Max(minBufferCount, 1));

            try
            {
                while (forceOne || _deleteBuffer.Count >= minBufferCount)
                {
                    forceOne = false;

                    var entries = EntriesToDelete(SqsQueueDefinition.MaxBatchDeleteItems).ToList();

                    if (entries.Count <= 0)
                    {
                        break;
                    }

                    deletedAtSqs = true;

                    var response = SqsClient.DeleteMessageBatch(new DeleteMessageBatchRequest
                                                                {
                                                                    QueueUrl = _queueDefinition.QueueUrl,
                                                                    Entries = entries
                                                                });

                    if (response.Failed != null && response.Failed.Count > 0)
                    {
                        response.Failed.Each(f => HandleError(f.ToException()));
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }

            return deletedAtSqs;
        }

        private IEnumerable<DeleteMessageBatchRequestEntry> EntriesToDelete(int count)
        {
            var result = new Dictionary<String, DeleteMessageBatchRequestEntry>(count);

            while (result.Count < count)
            {
                DeleteMessageRequest request;

                if (!_deleteBuffer.TryDequeue(out request))
                {
                    return result.Values;
                }

                var id = request.ReceiptHandle.ToSha256HashString64();

                if (!result.ContainsKey(id))
                {
                    result.Add(id, new DeleteMessageBatchRequestEntry
                                   {
                                       Id = id,
                                       ReceiptHandle = request.ReceiptHandle
                                   });
                }
            }

            return result.Values;
        }

        private Message BufferResponse(ReceiveMessageResponse response)
        {
            if (response == null || response.Messages == null)
            {
                return null;
            }

            Message toReturn = null;

            foreach (var message in response.Messages.Where(m => m != null))
            {
                if (toReturn == null)
                {
                    toReturn = message;
                    continue;
                }

                _receiveBuffer.Enqueue(message);
            }

            return toReturn;
        }

        public Message Receive(ReceiveMessageRequest request)
        {
            if (request == null)
            {
                return null;
            }

            Message toReturn = null;

            if (_receiveBuffer.Count > 0 && _receiveBuffer.TryDequeue(out toReturn))
            {
                return toReturn;
            }

            request.MaxNumberOfMessages = Math.Min(SqsQueueDefinition.MaxBatchReceiveItems, Math.Max(request.MaxNumberOfMessages, 1));

            var response = SqsClient.ReceiveMessage(request);

            return BufferResponse(response);
        }

        private void NakBufferedReceived()
        {
            Message toNak = null;

            var stopAt = DateTime.UtcNow.AddSeconds(30);

            while (_receiveBuffer.Count > 0 && DateTime.UtcNow <= stopAt && _receiveBuffer.TryDequeue(out toNak))
            {
                ChangeVisibility(new ChangeMessageVisibilityRequest
                                 {
                                     QueueUrl = _queueDefinition.QueueUrl,
                                     ReceiptHandle = toNak.ReceiptHandle,
                                     VisibilityTimeout = 0
                                 });
            }
        }

        public int DeleteBufferCount
        {
            get { return _deleteBuffer.Count; }
        }

        public int SendBufferCount
        {
            get { return _sendBuffer.Count; }
        }

        public int ChangeVisibilityBufferCount
        {
            get { return _cvBuffer.Count; }
        }

        public int ReceiveBufferCount
        {
            get { return _receiveBuffer.Count; }
        }
        
        public void Drain(bool fullDrain, bool nakReceived = false)
        {
            SendEnqueued(fullDrain ? 1 : SqsQueueDefinition.MaxBatchSendItems, forceOne: true);

            if (nakReceived)
            {
                NakBufferedReceived();
            }

            CvEnqueued(fullDrain ? 1 : SqsQueueDefinition.MaxBatchCvItems, forceOne: true);
            DeleteEnqueued(fullDrain ? 1 : SqsQueueDefinition.MaxBatchDeleteItems, forceOne: true);
        }

        public void Dispose()
        {   // Do our best to drain all the buffers
            Drain(fullDrain: true, nakReceived: true);

            if (_sqsClient == null)
            {
                return;
            }

            try
            {
                _sqsClient.Dispose();
                _sqsClient = null;
            }
            catch { }
        }
    }
}