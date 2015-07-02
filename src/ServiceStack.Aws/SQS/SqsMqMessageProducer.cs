using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using ServiceStack.Aws.Interfaces;
using ServiceStack.Logging;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.SQS
{
    public class SqsMqMessageProducer : IMessageProducer, IOneWayClient
    {
        protected static readonly ILog _log = LogManager.GetLogger(typeof(SqsMqMessageProducer));

        protected readonly ISqsMqBufferFactory _sqsMqBufferFactory;
        protected readonly SqsQueueManager _sqsQueueManager;

        public SqsMqMessageProducer(ISqsMqBufferFactory sqsMqBufferFactory, SqsQueueManager sqsQueueManager)
        {
            _sqsMqBufferFactory = sqsMqBufferFactory;
            _sqsQueueManager = sqsQueueManager;
        }

        public Action OnPublishedCallback { get; set; }
        
        public void Publish<T>(T messageBody)
        {
            var message = messageBody as IMessage;

            if (message != null)
            {
                Publish(message.ToInQueueName(), message);
            }
            else
            {
                Publish(new Message<T>(messageBody));
            }
        }

        public void Publish<T>(IMessage<T> message)
        {
            Publish(message.ToInQueueName(), message);
        }
        
        public void SendOneWay(object requestDto)
        {
            Publish(MessageFactory.Create(requestDto));
        }

        public void SendOneWay(string queueName, object requestDto)
        {
            Publish(queueName, MessageFactory.Create(requestDto));
        }

        public void SendAllOneWay(IEnumerable<object> requests)
        {
            if (requests == null)
            {
                return;
            }

            foreach (var request in requests)
            {
                SendOneWay(request);
            }
        }

        public void Publish(string queueName, IMessage message)
        {
            using(__requestAccess())
            {
                var queueDefinition = _sqsQueueManager.GetOrCreate(queueName);

                var sqsBuffer = _sqsMqBufferFactory.GetOrCreate(queueDefinition);

                sqsBuffer.Send(new SendMessageRequest
                               {
                                   QueueUrl = queueDefinition.QueueUrl,
                                   MessageBody = message.ToJsv()
                               });

                if (OnPublishedCallback != null)
                {
                    OnPublishedCallback();
                }
            }
        }

        private class AccessToken
        {
            private string _token;

            internal static readonly AccessToken __accessToken = new AccessToken("lUjBZNG56eE9yd3FQdVFSTy9qeGl5dlI5RmZwamc4U05udl000");

            private AccessToken(string token)
            {
                _token = token;
            }
        }

        private class DummyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        protected IDisposable __requestAccess()
        {
            var ra = new DummyDisposable();
            //return LicenseUtils.RequestAccess(AccessToken.__accessToken, LicenseFeature.Client, LicenseFeature.Text);
            return ra;
        }

        public void Dispose()
        {
            // NOTE: Do not dispose the bufferFactory or queueManager here, this object didn't create them, it was given them
        }

    }
}