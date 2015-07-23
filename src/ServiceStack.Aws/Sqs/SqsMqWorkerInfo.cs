using System;
using ServiceStack.Aws.Support;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqWorkerInfo
    {
        private int visibilityTimeout;
        private int receiveWaitTime;
        private Type messageType;
        
        public IMessageHandlerFactory MessageHandlerFactory { get; set; }
        public int ThreadCount { get; set; }
        public int RetryCount { get; set; }
        
        public Type MessageType
        {
            get { return messageType; }
            set
            {
                messageType = value;
                QueueNames = new QueueNames(messageType);
            }
        }

        public bool DisableBuffering { get; set; }
        public QueueNames QueueNames { get; private set; }
        
        public int VisibilityTimeout
        {
            get { return visibilityTimeout; }
            set
            {
                Guard.AgainstArgumentOutOfRange(value < 0 || value > SqsQueueDefinition.MaxVisibilityTimeoutSeconds, "SQS MQ VisibilityTimeout must be 0-43200");
                visibilityTimeout = value;
            }
        }

        public int ReceiveWaitTime
        {
            get { return receiveWaitTime; }
            set
            {
                Guard.AgainstArgumentOutOfRange(value < 0 || value > SqsQueueDefinition.MaxWaitTimeSeconds, "SQS MQ ReceiveWaitTime must be 0-20");
                receiveWaitTime = value;
            }
        }
    }
}
