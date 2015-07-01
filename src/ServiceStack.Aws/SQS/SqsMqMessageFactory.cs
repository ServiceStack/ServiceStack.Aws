using System;
using ServiceStack.Aws.Interfaces;
using ServiceStack.Aws.Support;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.SQS
{
    public class SqsMqMessageFactory : ISqsMqMessageFactory
    {
        private readonly ISqsMqBufferFactory _sqsMqBufferFactory;
        private readonly SqsQueueManager _sqsQueueManager;
        private int _retryCount;

        public SqsMqMessageFactory(SqsQueueManager sqsQueueManager) 
            : this(new SqsMqBufferFactory(sqsQueueManager.ConnectionFactory), sqsQueueManager) { }

        public SqsMqMessageFactory(ISqsMqBufferFactory sqsMqBufferFactory,
                                   SqsQueueManager sqsQueueManager)
        {
            Guard.AgainstNullArgument(sqsMqBufferFactory, "sqsMqBufferFactory");
            Guard.AgainstNullArgument(sqsQueueManager, "sqsQueueManager");
            
            _sqsMqBufferFactory = sqsMqBufferFactory;
            _sqsQueueManager = sqsQueueManager;
        }

        public SqsQueueManager QueueManager
        {
            get { return _sqsQueueManager; }
        }
    
        public SqsConnectionFactory ConnectionFactory
        {
            get { return _sqsQueueManager.ConnectionFactory; }
        }

        public Action<Exception> ErrorHandler
        {
            get { return _sqsMqBufferFactory.ErrorHandler; }
            set { _sqsMqBufferFactory.ErrorHandler = value; }
        }

        public int BufferFlushIntervalSeconds
        {
            get { return _sqsMqBufferFactory.BufferFlushIntervalSeconds; }
            set { _sqsMqBufferFactory.BufferFlushIntervalSeconds = value; }
        }

        public int RetryCount
        {
            get { return _retryCount; }
            set
            {
                Guard.AgainstArgumentOutOfRange(value < 0 || value > 1000, "SQS MQ RetryCount must be 0-1000");
                _retryCount = value;
            }
        }
        
        public void Dispose()
        {
            try
            {
                _sqsQueueManager.Dispose();
            }
            catch { }
            
            try
            {
                _sqsMqBufferFactory.Dispose();
            }
            catch { }

            
        }

        public IMessageQueueClient CreateMessageQueueClient()
        {
            return new SqsMqClient(_sqsMqBufferFactory, _sqsQueueManager);
        }

        public IMessageProducer CreateMessageProducer()
        {
            return new SqsMqMessageProducer(_sqsMqBufferFactory, _sqsQueueManager);
        }
    }
}