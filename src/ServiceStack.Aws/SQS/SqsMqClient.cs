using System;
using Amazon.SQS.Model;
using ServiceStack.Aws.Interfaces;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.SQS
{
    public class SqsMqClient : SqsMqMessageProducer, IMessageQueueClient
    {
        public SqsMqClient(ISqsMqBufferFactory sqsMqBufferFactory, SqsQueueManager sqsQueueManager)
            : base(sqsMqBufferFactory, sqsQueueManager) { }

        public void Notify(string queueName, IMessage message)
        {   // Just a publish
            Publish(queueName, message);
        }

        public IMessage<T> Get<T>(string queueName, TimeSpan? timeOut = null)
        {
            return GetMessage<T>(queueName, timeOut.HasValue
                                                ? (Int32)Math.Round(timeOut.Value.TotalSeconds, MidpointRounding.AwayFromZero)
                                                : -1);
        }

        public IMessage<T> GetAsync<T>(string queueName)
        {
            return GetMessage<T>(queueName, waitSeconds: 0);
        }

        private IMessage<T> GetMessage<T>(String queueName, Int32 waitSeconds)
        {
            using (__requestAccess())
            {
                var receiveWaitTime = waitSeconds < 0
                                          ? SqsQueueDefinition.MaxWaitTimeSeconds
                                          : SqsQueueDefinition.GetValidQueueWaitTime(waitSeconds);

                var queueDefinition = _sqsQueueManager.GetOrCreate(queueName, receiveWaitTimeSeconds: receiveWaitTime);
                
                var timeoutAt = waitSeconds >= 0
                                    ? DateTime.UtcNow.AddSeconds(waitSeconds)
                                    : DateTime.MaxValue;

                var sqsBuffer = _sqsMqBufferFactory.GetOrCreate(queueDefinition);

                do
                {
                    var sqsMessage = sqsBuffer.Receive(new ReceiveMessageRequest
                                                       {
                                                           MaxNumberOfMessages = queueDefinition.ReceiveBufferSize,
                                                           QueueUrl = queueDefinition.QueueUrl,
                                                           VisibilityTimeout = queueDefinition.VisibilityTimeout,
                                                           WaitTimeSeconds = receiveWaitTime
                                                       });

                    var message = sqsMessage.ToMessage<T>(queueDefinition.QueueName);

                    if (message != null)
                    {
                        return message;
                    }

                } while (DateTime.UtcNow <= timeoutAt);
            }

            return null;
        }

        public void DeleteMessage(IMessage message)
        {
            if (message == null || String.IsNullOrEmpty(message.Tag))
            {
                return;
            }

            var sqsTag = message.Tag.FromJsv<SqsMessageTag>();

            var queueDefinition = _sqsQueueManager.GetOrCreate(sqsTag.QName);

            var sqsBuffer = _sqsMqBufferFactory.GetOrCreate(queueDefinition);

            sqsBuffer.Delete(new DeleteMessageRequest
                             {
                                 QueueUrl = queueDefinition.QueueUrl,
                                 ReceiptHandle = sqsTag.RHandle
                             });
        }

        public void Ack(IMessage message)
        {
            DeleteMessage(message);
        }

        public void ChangeVisibility(IMessage message, Int32 visibilityTimeoutSeconds)
        {
            if (message == null || String.IsNullOrEmpty(message.Tag))
            {
                return;
            }

            var sqsTag = message.Tag.FromJsv<SqsMessageTag>();

            var queueDefinition = _sqsQueueManager.GetOrCreate(sqsTag.QName);

            var sqsBuffer = _sqsMqBufferFactory.GetOrCreate(queueDefinition);

            sqsBuffer.ChangeVisibility(new ChangeMessageVisibilityRequest
                                       {
                                           QueueUrl = queueDefinition.QueueUrl,
                                           ReceiptHandle = sqsTag.RHandle,
                                           VisibilityTimeout = visibilityTimeoutSeconds
                                       });
        }

        public void Nak(IMessage message, bool requeue, Exception exception = null)
        {
            if (requeue)
            {   // NOTE: Cannot simply cv at SQS, as that simply puts the same message with the same state back on the q at
                // SQS, and we need the state on the message object coming in to this Nak to remain with it (i.e. retryCount, etc.)
                //ChangeVisibility(message, 0);

                DeleteMessage(message);
                Publish(message);
            }
            else
            {
                try
                {
                    Publish(message.ToDlqQueueName(), message);
                    DeleteMessage(message);
                }
                catch(Exception ex)
                {
                    _log.Debug("Error trying to Nak message to Dlq", ex);
                    ChangeVisibility(message, 0);
                }
            }
        }

        public IMessage<T> CreateMessage<T>(object mqResponse)
        {
            using(__requestAccess())
            {
                return (IMessage<T>)mqResponse;
            }
        }

        public string GetTempQueueName()
        {   // NOTE: Purposely not creating DLQ queues for all these temps if they get used, they'll get
            // created on the fly as needed if messages actually fail
            var queueDefinition = _sqsQueueManager.GetOrCreate(QueueNames.GetTempQueueName());
            return queueDefinition.QueueName;
        }

    }
}