using System;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.Sqs
{
    public interface ISqsMqMessageFactory : IMessageFactory
    {
        ISqsQueueManager QueueManager { get; }
        SqsConnectionFactory ConnectionFactory { get; }
        int RetryCount { get; set; }
        int BufferFlushIntervalSeconds { get; set; }
        Action<Exception> ErrorHandler { get; set; }
    }
}