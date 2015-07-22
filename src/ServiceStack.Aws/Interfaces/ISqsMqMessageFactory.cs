using System;
using ServiceStack.Aws.Sqs;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.Interfaces
{
    public interface ISqsMqMessageFactory : IMessageFactory
    {
        SqsQueueManager QueueManager { get; }
        SqsConnectionFactory ConnectionFactory { get; }
        int RetryCount { get; set; }
        int BufferFlushIntervalSeconds { get; set; }
        Action<Exception> ErrorHandler { get; set; }
    }
}