using System;
using Amazon.SQS.Model;
using ServiceStack.Aws.Sqs;

namespace ServiceStack.Aws.Interfaces
{
    public interface ISqsMqBuffer : IDisposable
    {
        SqsQueueDefinition QueueDefinition { get; }
        bool Send(SendMessageRequest request);
        int SendBufferCount { get; }
        Message Receive(ReceiveMessageRequest request);
        int ReceiveBufferCount { get; }
        bool Delete(DeleteMessageRequest request);
        int DeleteBufferCount { get; }
        bool ChangeVisibility(ChangeMessageVisibilityRequest request);
        int ChangeVisibilityBufferCount { get; }
        void Drain(bool fullDrain, bool nakReceived = false);
        Action<Exception> ErrorHandler { get; set; }
    }
}