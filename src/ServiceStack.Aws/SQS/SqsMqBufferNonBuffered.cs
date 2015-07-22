using System;
using System.Linq;
using Amazon.SQS;
using Amazon.SQS.Model;
using ServiceStack.Aws.Interfaces;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqBufferNonBuffered : ISqsMqBuffer
    {
        private readonly SqsQueueDefinition _queueDefinition;
        private readonly SqsConnectionFactory _sqsConnectionFactory;
        private IAmazonSQS _sqsClient;

        public SqsMqBufferNonBuffered(SqsQueueDefinition queueDefinition,
                                      SqsConnectionFactory sqsConnectionFactory)
        {
            Guard.AgainstNullArgument(queueDefinition, "queueDefinition");
            Guard.AgainstNullArgument(sqsConnectionFactory, "sqsConnectionFactory");

            _queueDefinition = queueDefinition;
            _sqsConnectionFactory = sqsConnectionFactory;
        }

        private IAmazonSQS SqsClient
        {
            get { return _sqsClient ?? (_sqsClient = _sqsConnectionFactory.GetClient()); }
        }

        public SqsQueueDefinition QueueDefinition
        {
            get { return _queueDefinition; }
        }
        
        public Action<Exception> ErrorHandler { get; set; }
        
        public Boolean Delete(DeleteMessageRequest request)
        {
            if (request == null)
            {
                return false;
            }

            var response = SqsClient.DeleteMessage(request);
            return response != null;
        }
        
        public bool ChangeVisibility(ChangeMessageVisibilityRequest request)
        {
            if (request == null)
            {
                return false;
            }

            var response = SqsClient.ChangeMessageVisibility(request);
            return response != null;
        }
        
        public Boolean Send(SendMessageRequest request)
        {
            if (request == null)
            {
                return false;
            }

            var response = SqsClient.SendMessage(request);
            return response != null;
        }
        
        public Message Receive(ReceiveMessageRequest request)
        {
            if (request == null)
            {
                return null;
            }

            request.MaxNumberOfMessages = 1;

            var response = SqsClient.ReceiveMessage(request);

            return response == null
                       ? null
                       : response.Messages.SingleOrDefault();
        }

        public int DeleteBufferCount
        {
            get { return 0; }
        }

        public int SendBufferCount
        {
            get { return 0; }
        }

        public int ChangeVisibilityBufferCount
        {
            get { return 0; }
        }

        public int ReceiveBufferCount
        {
            get { return 0; }
        }

        public void Drain(bool fullDrain, bool nakReceived = false)
        {
        }

        public void Dispose()
        {
            if (_sqsClient == null)
            {
                return;
            }

            try
            {
                _sqsClient.Dispose();
                _sqsClient = null;
            }
            catch { }

        }
    }
}