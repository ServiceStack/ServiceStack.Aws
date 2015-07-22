using System;
using ServiceStack.Aws.Support;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqWorkerInfo
    {
        private int _visibilityTimeout;
        private int _receiveWaitTime;
        private Type _messageType;
        
        public IMessageHandlerFactory MessageHandlerFactory { get; set; }
        public Int32 ThreadCount { get; set; }
        public Int32 RetryCount { get; set; }
        
        public Type MessageType
        {
            get { return _messageType; }
            set
            {
                _messageType = value;
                QueueNames = new QueueNames(_messageType);
            }
        }

        public bool DisableBuffering { get; set; }
        public QueueNames QueueNames { get; private set; }
        
        public int VisibilityTimeout
        {
            get { return _visibilityTimeout; }
            set
            {
                Guard.AgainstArgumentOutOfRange(value < 0 || value > SqsQueueDefinition.MaxVisibilityTimeoutSeconds, "SQS MQ VisibilityTimeout must be 0-43200");
                _visibilityTimeout = value;
            }
        }

        public int ReceiveWaitTime
        {
            get { return _receiveWaitTime; }
            set
            {
                Guard.AgainstArgumentOutOfRange(value < 0 || value > SqsQueueDefinition.MaxWaitTimeSeconds, "SQS MQ ReceiveWaitTime must be 0-20");
                _receiveWaitTime = value;
            }
        }

    }
}
