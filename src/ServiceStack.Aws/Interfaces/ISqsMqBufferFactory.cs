using System;
using ServiceStack.Aws.Sqs;

namespace ServiceStack.Aws.Interfaces
{
    public interface ISqsMqBufferFactory : IDisposable
    {
        ISqsMqBuffer GetOrCreate(SqsQueueDefinition queueDefinition);
        int BufferFlushIntervalSeconds { get; set; }
        Action<Exception> ErrorHandler { get; set; }
    }
}