using System;
using Amazon.SQS.Model;
using ServiceStack.Aws.SQS;

namespace ServiceStack.Aws.Interfaces
{
    public interface ISqsMqBuffer : IDisposable
    {
        SqsQueueDefinition QueueDefinition { get; }
        Boolean Send(SendMessageRequest request);
        int SendBufferCount { get; }
        Message Receive(ReceiveMessageRequest request);
        int ReceiveBufferCount { get; }
        Boolean Delete(DeleteMessageRequest request);
        int DeleteBufferCount { get; }
        Boolean ChangeVisibility(ChangeMessageVisibilityRequest request);
        int ChangeVisibilityBufferCount { get; }
        void Drain(bool fullDrain, bool nakReceived = false);
        Action<Exception> ErrorHandler { get; set; }
    }
}