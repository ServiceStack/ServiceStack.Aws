using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Amazon.SQS;
using Amazon.SQS.Model;
using NUnit.Framework;
using ServiceStack.Aws.SQS;
using ServiceStack.Aws.SQS.Fake;

namespace ServiceStack.Aws.Tests.SQS
{
    public class FakeSqsClientHelper
    {
        private class FakeSqsModel
        {
            public FakeSqsModel()
            {
                var dt = DateTime.UtcNow;

                Id = Guid.NewGuid().ToString("N");
                Name = "Name for {0}".Fmt(Id);
                DecValue = 12340.56789m;
                IntValue = dt.Ticks;
                DtValue = dt;
            }

            public string Id { get; set; }
            public string Name { get; set; }
            public decimal DecValue { get; set; }
            public long IntValue { get; set; }
            public DateTime DtValue { get; set; }

            public static string CreateJsv()
            {
                return new FakeSqsModel().ToJsv();
            }

        }

        private readonly IAmazonSQS _client;
        private readonly string _defaultQueueUrl;

        public FakeSqsClientHelper() : this(FakeAmazonSqs.Instance) { }

        public FakeSqsClientHelper(IAmazonSQS client)
        {
            _client = client;
            _defaultQueueUrl = CreateQueue();
        }

        public string DefaultQueueUrl
        {
            get { return _defaultQueueUrl; }
        }
        
        public int SendMessages(string queueUrl = null, int count = 1)
        {
            if (queueUrl == null)
            {
                queueUrl = _defaultQueueUrl;
            }

            if (count <= 1)
            {
                var scalarResponse = _client.SendMessage(queueUrl, FakeSqsModel.CreateJsv());
                
                return String.IsNullOrEmpty(scalarResponse.MessageId)
                           ? 0
                           : 1;
            }

            var request = new SendMessageBatchRequest
                          {
                              QueueUrl = queueUrl,
                              Entries = new List<SendMessageBatchRequestEntry>(count)
                          };

            for (var x = 0; x < count; x++)
            {
                var model = new FakeSqsModel();

                request.Entries.Add(new SendMessageBatchRequestEntry
                                    {
                                        Id = model.Id,
                                        MessageBody = model.ToJsv()
                                    });
            }

            var response = _client.SendMessageBatch(request);

            return response.Successful.Count;
        }

        public Message ReceiveSingle(string queueUrl = null, int visTimeout = 0)
        {
            if (queueUrl == null)
            {
                queueUrl = _defaultQueueUrl;
            }

            var maxAttempts = SqsTestAssert.IsFakeClient
                                  ? 1
                                  : 5;

            var receiveAttempts = 0;

            ReceiveMessageResponse received = null;

            while (receiveAttempts < maxAttempts)
            {
                received = _client.ReceiveMessage(new ReceiveMessageRequest
                                                  {
                                                      QueueUrl = queueUrl,
                                                      MaxNumberOfMessages = 1,
                                                      VisibilityTimeout = visTimeout > 0
                                                                              ? visTimeout
                                                                              : SqsQueueDefinition.MaxVisibilityTimeoutSeconds,
                                                      WaitTimeSeconds = SqsTestAssert.IsFakeClient
                                                                            ? 0
                                                                            : 3
                                                  });

                if (received != null)
                {
                    break;
                }

                receiveAttempts++;
            }
            
            Assert.IsNotNull(received);
            Assert.IsNotNull(received.Messages);
            Assert.AreEqual(1, received.Messages.Count);

            return received.Messages.Single();
        }

        public string CreateQueue(string queueName = null, int visTimeout = 24, int waitTime = 14)
        {
            if (queueName == null)
            {
                queueName = Guid.NewGuid().ToString("N");
            }

            var createRequest = new CreateQueueRequest
                                {
                                    QueueName = queueName,
                                    Attributes = new Dictionary<string, string>
                                                 {
                                                     { QueueAttributeName.VisibilityTimeout, visTimeout.ToString(CultureInfo.InvariantCulture) },
                                                     { QueueAttributeName.ReceiveMessageWaitTimeSeconds, waitTime.ToString(CultureInfo.InvariantCulture) },
                                                 }
                                };

            var createResponse = _client.CreateQueue(createRequest);

            Assert.IsNotNull(createResponse);
            Assert.IsNotNullOrEmpty(createResponse.QueueUrl);

            return createResponse.QueueUrl;
        }

    }
}