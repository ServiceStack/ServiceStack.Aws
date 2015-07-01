using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using NUnit.Framework;
using ServiceStack.Aws.SQS;

namespace ServiceStack.Aws.Tests.SQS
{
    [TestFixture]
    public class FakeAmazonSqsTests
    {
        private IAmazonSQS _client;
        private FakeSqsClientHelper _helper;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            _client = SqsTestClientFactory.GetClient();
            _helper = new FakeSqsClientHelper(_client);
        }

        [Test]
        public void Create_fails_with_non_alphanumeric_chars()
        {
            Assert.Throws<AmazonSQSException>(() => _client.CreateQueue("testing123:testing456"));
            Assert.Throws<AmazonSQSException>(() => _client.CreateQueue("testing123.testing456"));
            Assert.Throws<AmazonSQSException>(() => _client.CreateQueue("testing123=testing456"));
            Assert.Throws<AmazonSQSException>(() => _client.CreateQueue("testing123+testing456"));
        }

        [Test]
        public void Create_succeeds_with_valid_non_alphanumeric_chars()
        {
            var createResponse = _client.CreateQueue("testing123-testing456");
            Assert.IsNotNullOrEmpty(createResponse.QueueUrl);

            createResponse = _client.CreateQueue("testing123_testing456");
            Assert.IsNotNullOrEmpty(createResponse.QueueUrl);
            
            createResponse = _client.CreateQueue("-_");
            Assert.IsNotNullOrEmpty(createResponse.QueueUrl);
        }

        [Test]
        public void Can_create_and_get_attributes_and_url_correctly()
        {
            var createRequest = new CreateQueueRequest
                          {
                              QueueName = Guid.NewGuid().ToString("N"),
                              Attributes = new Dictionary<string, string>
                                           {
                                               { QueueAttributeName.VisibilityTimeout, "23" },
                                               { QueueAttributeName.ReceiveMessageWaitTimeSeconds, "13" },
                                           }
                          };

            var createResponse = _client.CreateQueue(createRequest);

            Assert.IsNotNullOrEmpty(createResponse.QueueUrl);

            var attrResponse = _client.GetQueueAttributes(createResponse.QueueUrl, new List<string>
                                                                                   {
                                                                                       "All"
                                                                                   });

            Assert.AreEqual(attrResponse.Attributes[QueueAttributeName.VisibilityTimeout],
                            createRequest.Attributes[QueueAttributeName.VisibilityTimeout]);
            Assert.AreEqual(attrResponse.Attributes[QueueAttributeName.ReceiveMessageWaitTimeSeconds],
                            createRequest.Attributes[QueueAttributeName.ReceiveMessageWaitTimeSeconds]);

            var qUrlResponse = _client.GetQueueUrl(createRequest.QueueName);

            Assert.AreEqual(qUrlResponse.QueueUrl, createResponse.QueueUrl);
        }

        [Test]
        public void Creating_duplicate_queue_names_throws_exception()
        {
            var name = Guid.NewGuid().ToString("N");

            _helper.CreateQueue(name);

            Assert.Throws<QueueNameExistsException>(() => _helper.CreateQueue(name));
        }

        [Test]
        public void Can_send_single_message()
        {
            Assert.AreEqual(1, _helper.SendMessages());
        }

        [Test]
        public void Can_send_batch_of_messages()
        {
            Assert.AreEqual(9, _helper.SendMessages(count: 9));
        }

        [Test]
        public void Sending_too_many_messages_throws_exception()
        {
            Assert.Throws<TooManyEntriesInBatchRequestException>(() => _helper.SendMessages(count: (SqsQueueDefinition.MaxBatchSendItems + 1)));
        }

        [Test]
        public void Sending_no_entries_throws_exception()
        {
            Assert.Throws<EmptyBatchRequestException>(() => _client.SendMessageBatch(new SendMessageBatchRequest
                                                                                     {
                                                                                         Entries = new List<SendMessageBatchRequestEntry>()
                                                                                     }));
        }

        [Test]
        public void Sending_to_non_existent_q_throws_exception()
        {
            Assert.Throws<QueueDoesNotExistException>(() => _helper.SendMessages(queueUrl: Guid.NewGuid().ToString("N")));
        }

        [Test]
        public void Sending_duplicate_entries_throws_exception()
        {
            var id = Guid.NewGuid().ToString("N");

            Assert.Throws<BatchEntryIdsNotDistinctException>(() => _client.SendMessageBatch(new SendMessageBatchRequest
                                                                                            {
                                                                                                QueueUrl = _helper.DefaultQueueUrl,
                                                                                                Entries = new List<SendMessageBatchRequestEntry>
                                                                                                          {
                                                                                                              new SendMessageBatchRequestEntry
                                                                                                              {
                                                                                                                  Id = id,
                                                                                                                  MessageBody = id
                                                                                                              },
                                                                                                              new SendMessageBatchRequestEntry
                                                                                                              {
                                                                                                                  Id = Guid.NewGuid().ToString("N"),
                                                                                                                  MessageBody = Guid.NewGuid().ToString("N")
                                                                                                              },
                                                                                                              new SendMessageBatchRequestEntry
                                                                                                              {
                                                                                                                  Id = id,
                                                                                                                  MessageBody = id
                                                                                                              }
                                                                                                          }
                                                                                            }));
        }

        [Test]
        public void Changing_visibility_on_valid_item_succeeds()
        {
            _helper.SendMessages(count: 4);

            var message = _helper.ReceiveSingle();

            var response = _client.ChangeMessageVisibility(_helper.DefaultQueueUrl, message.ReceiptHandle, 28);

            Assert.IsNotNull(response);
        }
        
        [Test]
        public void Changing_visibility_to_zero_on_valid_item_requeues_it()
        {
            var newQueueUrl = _helper.CreateQueue();

            // New q should be empty
            var response = _client.ReceiveMessage(newQueueUrl);
            Assert.AreEqual(0, response.Messages.Count);

            // Send 1, pull it off (should only get 1)
            _helper.SendMessages(newQueueUrl);

            var message = _helper.ReceiveSingle(newQueueUrl);

            // Q should be empty again
            response = _client.ReceiveMessage(newQueueUrl);
            Assert.AreEqual(0, response.Messages.Count);

            // CV on message we have
            _client.ChangeMessageVisibility(newQueueUrl, message.ReceiptHandle, 0);

            // Should be a single item (same one) back on q again
            var messageRepeat = _helper.ReceiveSingle(newQueueUrl);
            Assert.AreEqual(message.MessageId, messageRepeat.MessageId);
        }

        [Test]
        public void Changing_visibility_on_non_existent_item_throws_exception()
        {
            _helper.SendMessages(count: 4);

            Assert.Throws<ReceiptHandleIsInvalidException>(
                () => _client.ChangeMessageVisibility(_helper.DefaultQueueUrl, Guid.NewGuid().ToString("N"), 11));
        }
        
        [Test]
        public void Received_message_no_ack_gets_requeued()
        {
            var newQueueUrl = _helper.CreateQueue();

            // New q should be empty
            var response = _client.ReceiveMessage(newQueueUrl);
            Assert.AreEqual(0, response.Messages.Count);

            // Send 1, pull it off (should only get 1)
            _helper.SendMessages(newQueueUrl);
            var message = _helper.ReceiveSingle(newQueueUrl, visTimeout: 1);
            
            // Q should be empty again
            response = _client.ReceiveMessage(newQueueUrl);
            Assert.AreEqual(0, response.Messages.Count);

            Thread.Sleep(1000);

            // Should be a single item (same one) back on q again
            var messageRepeat = _helper.ReceiveSingle(newQueueUrl);
            Assert.AreEqual(message.MessageId, messageRepeat.MessageId);
        }

        [Test]
        public void Deleting_non_existent_item_throws_exception()
        {
            Assert.Throws<ReceiptHandleIsInvalidException>(() => _client.DeleteMessage(_helper.DefaultQueueUrl, Guid.NewGuid().ToString("N")));
        }

        [Test]
        public void Deleting_valid_item_succeeds()
        {
            _helper.SendMessages(count: 2);

            var message = _helper.ReceiveSingle();

            var success = _client.DeleteMessage(_helper.DefaultQueueUrl, message.ReceiptHandle);

            Assert.IsNotNull(success);
        }

        [Test]
        public void Can_delete_batch_of_messages()
        {
            var newQueueUrl = _helper.CreateQueue();

            _helper.SendMessages(newQueueUrl, count: 6);

            var received = _client.ReceiveMessage(new ReceiveMessageRequest
                                                  {
                                                      QueueUrl = newQueueUrl,
                                                      MaxNumberOfMessages = 5,
                                                      VisibilityTimeout = 30,
                                                      WaitTimeSeconds = 0
                                                  });

            Assert.AreEqual(5, received.Messages.Count);

            var response = _client.DeleteMessageBatch(newQueueUrl,
                                                      received.Messages
                                                              .Select(m => new DeleteMessageBatchRequestEntry
                                                                           {
                                                                               Id = m.MessageId,
                                                                               ReceiptHandle = m.ReceiptHandle
                                                                           })
                                                              .ToList());

            Assert.AreEqual(5, response.Successful.Count);

            received = _client.ReceiveMessage(new ReceiveMessageRequest
                                              {
                                                  QueueUrl = newQueueUrl,
                                                  MaxNumberOfMessages = 5,
                                                  VisibilityTimeout = 30,
                                                  WaitTimeSeconds = 0
                                              });

            Assert.AreEqual(1, received.Messages.Count);
        }

        [Test]
        public void Deleting_too_many_messages_throws_exception()
        {
            var entries = (SqsQueueDefinition.MaxBatchDeleteItems + 1).Times(() => new DeleteMessageBatchRequestEntry());

            Assert.Throws<TooManyEntriesInBatchRequestException>(() => _client.DeleteMessageBatch(new DeleteMessageBatchRequest
                                                                                                  {
                                                                                                      QueueUrl = _helper.DefaultQueueUrl,
                                                                                                      Entries = entries
                                                                                                  }));
        }

        [Test]
        public void Deleting_no_entries_throws_exception()
        {
            Assert.Throws<EmptyBatchRequestException>(() => _client.DeleteMessageBatch(new DeleteMessageBatchRequest
                                                                                       {
                                                                                           Entries = new List<DeleteMessageBatchRequestEntry>()
                                                                                       }));
        }

        [Test]
        public void Deleting_from_non_existent_q_throws_exception()
        {
            var entries = 1.Times(() => new DeleteMessageBatchRequestEntry());
            Assert.Throws<QueueDoesNotExistException>(() => _client.DeleteMessageBatch(Guid.NewGuid().ToString("N"), entries));
        }

        [Test]
        public void Can_delete_existing_queue()
        {
            var qUrl = _helper.CreateQueue();

            var response = _client.DeleteQueue(qUrl);

            Assert.IsNotNull(response);
        }

        [Test]
        public void Deleting_non_existent_queue_ignores_quietly()
        {
            var response = _client.DeleteQueue(Guid.NewGuid().ToString("N"));
            Assert.IsNotNull(response);
        }

        [Test]
        public void Can_get_url_for_existing_q()
        {
            var qName = Guid.NewGuid().ToString("N");

            var qUrl = _helper.CreateQueue(qName);

            var response = _client.GetQueueUrl(qName);

            Assert.IsNotNull(response);
            Assert.AreEqual(qUrl, response.QueueUrl);
        }

        [Test]
        public void Getting_url_for_non_existent_queue_throws_exception()
        {
            Assert.Throws<QueueDoesNotExistException>(() => _client.GetQueueUrl(Guid.NewGuid().ToString("N")));
        }

        [Test]
        public void Receive_with_empty_queue_waits_time_specified()
        {
            var qUrl = _helper.CreateQueue();

            var sw = Stopwatch.StartNew();

            var response = _client.ReceiveMessage(new ReceiveMessageRequest
                                                  {
                                                      QueueUrl = qUrl,
                                                      MaxNumberOfMessages = 1,
                                                      VisibilityTimeout = 30,
                                                      WaitTimeSeconds = 2
                                                  });

            sw.Stop();

            Assert.AreEqual(0, response.Messages.Count);
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 2000);
        }

        [Test]
        public void Receive_with_non_empty_queue_waits_time_specified_for_max_num_messages()
        {
            var qUrl = _helper.CreateQueue();

            _helper.SendMessages(qUrl, count: 3);

            var sw = Stopwatch.StartNew();

            var response = _client.ReceiveMessage(new ReceiveMessageRequest
                                                  {
                                                      QueueUrl = qUrl,
                                                      MaxNumberOfMessages = 4,
                                                      VisibilityTimeout = 30,
                                                      WaitTimeSeconds = 2
                                                  });

            sw.Stop();

            Assert.AreEqual(3, response.Messages.Count);
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 2000);
        }
        
    }
}