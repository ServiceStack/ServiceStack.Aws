using System;
using ServiceStack.Aws.Support;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqMessageFactory : ISqsMqMessageFactory
    {
        private readonly ISqsMqBufferFactory sqsMqBufferFactory;
        private readonly ISqsQueueManager sqsQueueManager;
        private int retryCount;

        public SqsMqMessageFactory(ISqsQueueManager sqsQueueManager) 
            : this(new SqsMqBufferFactory(sqsQueueManager.ConnectionFactory), sqsQueueManager) { }

        public SqsMqMessageFactory(ISqsMqBufferFactory sqsMqBufferFactory,
                                   ISqsQueueManager sqsQueueManager)
        {
            Guard.AgainstNullArgument(sqsMqBufferFactory, "sqsMqBufferFactory");
            Guard.AgainstNullArgument(sqsQueueManager, "sqsQueueManager");
            
            this.sqsMqBufferFactory = sqsMqBufferFactory;
            this.sqsQueueManager = sqsQueueManager;
        }

        public ISqsQueueManager QueueManager => sqsQueueManager;

        public SqsConnectionFactory ConnectionFactory => sqsQueueManager.ConnectionFactory;

        public Action<Exception> ErrorHandler
        {
            get { return sqsMqBufferFactory.ErrorHandler; }
            set { sqsMqBufferFactory.ErrorHandler = value; }
        }

        public int BufferFlushIntervalSeconds
        {
            get { return sqsMqBufferFactory.BufferFlushIntervalSeconds; }
            set { sqsMqBufferFactory.BufferFlushIntervalSeconds = value; }
        }

        public int RetryCount
        {
            get { return retryCount; }
            set
            {
                Guard.AgainstArgumentOutOfRange(value < 0 || value > 1000, "SQS MQ RetryCount must be 0-1000");
                retryCount = value;
            }
        }
        
        public void Dispose()
        {
            new IDisposable[] { sqsQueueManager, sqsMqBufferFactory }.Dispose();
        }

        public IMessageQueueClient CreateMessageQueueClient()
        {
            return new SqsMqClient(sqsMqBufferFactory, sqsQueueManager);
        }

        public IMessageProducer CreateMessageProducer()
        {
            return new SqsMqMessageProducer(sqsMqBufferFactory, sqsQueueManager);
        }
    }
}