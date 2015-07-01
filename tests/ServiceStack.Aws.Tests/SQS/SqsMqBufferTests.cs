using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.SQS.Model;
using NUnit.Framework;
using ServiceStack.Aws.Interfaces;
using ServiceStack.Aws.SQS;
using ServiceStack.Aws.SQS.Fake;

namespace ServiceStack.Aws.Tests.SQS
{
    [TestFixture]
    public class SqsMqBufferTests
    {
        private SqsQueueManager _sqsQueueManager;
        private SqsMqBufferFactory _sqsMqBufferFactory;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            _sqsQueueManager = new SqsQueueManager(SqsTestClientFactory.GetConnectionFactory());
            _sqsMqBufferFactory = new SqsMqBufferFactory(SqsTestClientFactory.GetConnectionFactory());
        }

        [TestFixtureTearDown]
        public void FixtureTeardown()
        {   // Cleanup anything left cached that we tested with
            var queueNamesToDelete = new List<string>(_sqsQueueManager.QueueNameMap.Keys);

            foreach (var queueName in queueNamesToDelete)
            {
                try
                {
                    _sqsQueueManager.DeleteQueue(queueName);
                }
                catch { }
            }
        }

        private ISqsMqBuffer GetNewMqBuffer(int? visibilityTimeoutSeconds = null,
                                            int? receiveWaitTimeSeconds = null,
                                            bool? disasbleBuffering = null)
        {
            var qd = _sqsQueueManager.CreateQueue(GetNewId(), visibilityTimeoutSeconds, receiveWaitTimeSeconds, disasbleBuffering);
            var buffer = _sqsMqBufferFactory.GetOrCreate(qd);
            return buffer;
        }
        
        private string GetNewId()
        {
            return Guid.NewGuid().ToString("N");
        }

        [Test]
        public void Send_is_not_buffered_when_buffering_disabled()
        {
            var buffer = GetNewMqBuffer(disasbleBuffering: true);

            var sent = buffer.Send(new SendMessageRequest
                                   {
                                       QueueUrl = buffer.QueueDefinition.QueueUrl,
                                       MessageBody = GetNewId()
                                   });

            Assert.IsTrue(sent);
            Assert.AreEqual(0, buffer.SendBufferCount);
        }

        [Test]
        public void Send_is_buffered_when_buffering_enabled_and_disposing_drains()
        {
            var buffer = GetNewMqBuffer();

            var sent = buffer.Send(new SendMessageRequest
                                   {
                                       QueueUrl = buffer.QueueDefinition.QueueUrl,
                                       MessageBody = GetNewId()
                                   });

            Assert.IsFalse(sent);
            Assert.AreEqual(1, buffer.SendBufferCount, "Send did not buffer");

            buffer.Dispose();

            Assert.AreEqual(0, buffer.SendBufferCount, "Dispose did not drain");
        }

        [Test]
        public void Delete_is_not_buffered_when_buffering_disabled()
        {
            var buffer = GetNewMqBuffer(disasbleBuffering: true);

            Assert.Throws<ReceiptHandleIsInvalidException>(() => buffer.Delete(new DeleteMessageRequest
                                                                               {
                                                                                   QueueUrl = buffer.QueueDefinition.QueueUrl,
                                                                                   ReceiptHandle = GetNewId()
                                                                               }));
        }

        [Test]
        public void Delete_is_buffered_when_buffering_enabled_and_disposing_drains()
        {
            var buffer = GetNewMqBuffer();

            var deleted = buffer.Delete(new DeleteMessageRequest
                                        {
                                            QueueUrl = buffer.QueueDefinition.QueueUrl,
                                            ReceiptHandle = GetNewId()
                                        });

            Assert.IsFalse(deleted);
            Assert.AreEqual(1, buffer.DeleteBufferCount, "Delete did not buffer");

            buffer.Dispose();

            Assert.AreEqual(0, buffer.DeleteBufferCount, "Dispose did not drain");
        }

        [Test]
        public void Cv_is_not_buffered_when_buffering_disabled()
        {
            var buffer = GetNewMqBuffer(disasbleBuffering: true);

            Assert.Throws<ReceiptHandleIsInvalidException>(() => buffer.ChangeVisibility(new ChangeMessageVisibilityRequest
                                                                                         {
                                                                                             QueueUrl = buffer.QueueDefinition.QueueUrl,
                                                                                             ReceiptHandle = GetNewId(),
                                                                                             VisibilityTimeout = 0
                                                                                         }));
        }

        [Test]
        public void Cv_is_buffered_when_buffering_enabled_and_disposing_drains()
        {
            var buffer = GetNewMqBuffer();

            var visChanged = buffer.ChangeVisibility(new ChangeMessageVisibilityRequest
                                                     {
                                                         QueueUrl = buffer.QueueDefinition.QueueUrl,
                                                         ReceiptHandle = GetNewId(),
                                                         VisibilityTimeout = 0
                                                     });

            Assert.IsFalse(visChanged);
            Assert.AreEqual(1, buffer.ChangeVisibilityBufferCount, "CV did not buffer");

            buffer.Dispose();

            Assert.AreEqual(0, buffer.ChangeVisibilityBufferCount, "Dispose did not drain");
        }

        [Test]
        public void Receive_is_not_buffered_when_buffering_disabled()
        {
            var buffer = GetNewMqBuffer(disasbleBuffering: true);

            var body = GetNewId();

            var sent = buffer.Send(new SendMessageRequest
                                   {
                                       QueueUrl = buffer.QueueDefinition.QueueUrl,
                                       MessageBody = body
                                   });

            Assert.IsTrue(sent);
            Assert.AreEqual(0, buffer.SendBufferCount);

            var received = buffer.Receive(new ReceiveMessageRequest
                                          {
                                              QueueUrl = buffer.QueueDefinition.QueueUrl,
                                              MaxNumberOfMessages = 5
                                          });

            Assert.IsNotNull(received);
            Assert.AreEqual(body, received.Body);
            Assert.AreEqual(0, buffer.ReceiveBufferCount);
        }

        [Test]
        public void Receive_is_buffered_when_buffering_enabled_and_disposing_drains()
        {
            var buffer = GetNewMqBuffer();

            buffer.QueueDefinition.SendBufferSize = 1;

            3.Times(i =>
                    {
                        var sent = buffer.Send(new SendMessageRequest
                                               {
                                                   QueueUrl = buffer.QueueDefinition.QueueUrl,
                                                   MessageBody = GetNewId()
                                               });

                        Assert.IsTrue(sent);
                        Assert.AreEqual(0, buffer.SendBufferCount);
                    });

            var received = buffer.Receive(new ReceiveMessageRequest
                                          {
                                              QueueUrl = buffer.QueueDefinition.QueueUrl,
                                              MaxNumberOfMessages = 5
                                          });

            Assert.IsNotNull(received);
            Assert.AreEqual(2, buffer.ReceiveBufferCount);

            buffer.Dispose();

            Assert.AreEqual(0, buffer.ReceiveBufferCount, "Dispose did not drain");
        }

        [Test]
        public void ErrorHandler_is_called_for_failed_batch_send_items()
        {
            var buffer = GetNewMqBuffer();

            var itemsErrorHandled = new List<Exception>();

            buffer.ErrorHandler = itemsErrorHandled.Add;
            buffer.QueueDefinition.SendBufferSize = 5;

            3.Times(i => buffer.Send(new SendMessageRequest
                                     {
                                         QueueUrl = buffer.QueueDefinition.QueueUrl,
                                         MessageBody = GetNewId()
                                     }));

            buffer.Send(new SendMessageRequest
                        {
                            QueueUrl = buffer.QueueDefinition.QueueUrl,
                            MessageBody = FakeSqsQueue.FakeBatchItemFailString
                        });

            var sent = buffer.Send(new SendMessageRequest
                                   {
                                       QueueUrl = buffer.QueueDefinition.QueueUrl,
                                       MessageBody = FakeSqsQueue.FakeBatchItemFailString
                                   });
            
            Assert.IsTrue(sent);
            Assert.AreEqual(0, buffer.SendBufferCount);
            Assert.AreEqual(2, itemsErrorHandled.Count);
        }

        [Test]
        public void ErrorHandler_is_called_for_failed_batch_cv_items()
        {
            var buffer = GetNewMqBuffer();

            var itemsErrorHandled = new List<Exception>();

            buffer.ErrorHandler = itemsErrorHandled.Add;
            buffer.QueueDefinition.ChangeVisibilityBufferSize = 4;

            3.Times(i => buffer.ChangeVisibility(new ChangeMessageVisibilityRequest
                                                 {
                                                     QueueUrl = buffer.QueueDefinition.QueueUrl,
                                                     ReceiptHandle = GetNewId()
                                                 }));

            var sent = buffer.ChangeVisibility(new ChangeMessageVisibilityRequest
                                               {
                                                   QueueUrl = buffer.QueueDefinition.QueueUrl,
                                                   ReceiptHandle = FakeSqsQueue.FakeBatchItemFailString
                                               });

            Assert.IsTrue(sent);
            Assert.AreEqual(0, buffer.ChangeVisibilityBufferCount);
            Assert.AreEqual(1, itemsErrorHandled.Count);
        }

        [Test]
        public void ErrorHandler_is_called_for_failed_batch_delete_items()
        {
            var buffer = GetNewMqBuffer();

            var itemsErrorHandled = new List<Exception>();

            buffer.ErrorHandler = itemsErrorHandled.Add;
            buffer.QueueDefinition.DeleteBufferSize = 4;

            3.Times(i => buffer.Delete(new DeleteMessageRequest
                                       {
                                           QueueUrl = buffer.QueueDefinition.QueueUrl,
                                           ReceiptHandle = GetNewId()
                                       }));

            var sent = buffer.Delete(new DeleteMessageRequest
                                     {
                                         QueueUrl = buffer.QueueDefinition.QueueUrl,
                                         ReceiptHandle = FakeSqsQueue.FakeBatchItemFailString
                                     });

            Assert.IsTrue(sent);
            Assert.AreEqual(0, buffer.DeleteBufferCount);
            Assert.AreEqual(1, itemsErrorHandled.Count);
        }

        [Test]
        public void Buffers_are_drained_on_timer_even_if_not_full()
        {
            var buffer = GetNewMqBuffer();

            2.Times(i => buffer.Send(new SendMessageRequest
                                     {
                                         QueueUrl = buffer.QueueDefinition.QueueUrl,
                                         MessageBody = GetNewId()
                                     }));
            4.Times(i => buffer.Delete(new DeleteMessageRequest
                                       {
                                           QueueUrl = buffer.QueueDefinition.QueueUrl,
                                           ReceiptHandle = GetNewId()
                                       }));
            3.Times(i => buffer.ChangeVisibility(new ChangeMessageVisibilityRequest
                                                 {
                                                     QueueUrl = buffer.QueueDefinition.QueueUrl,
                                                     ReceiptHandle = GetNewId()
                                                 }));

            var buffer2 = GetNewMqBuffer();

            3.Times(i => buffer2.Send(new SendMessageRequest
                                      {
                                          QueueUrl = buffer2.QueueDefinition.QueueUrl,
                                          MessageBody = GetNewId()
                                      }));
            2.Times(i => buffer2.Delete(new DeleteMessageRequest
                                        {
                                            QueueUrl = buffer2.QueueDefinition.QueueUrl,
                                            ReceiptHandle = GetNewId()
                                        }));
            4.Times(i => buffer2.ChangeVisibility(new ChangeMessageVisibilityRequest
                                                  {
                                                      QueueUrl = buffer2.QueueDefinition.QueueUrl,
                                                      ReceiptHandle = GetNewId()
                                                  }));

            var buffer3 = GetNewMqBuffer();

            4.Times(i => buffer3.Send(new SendMessageRequest
                                      {
                                          QueueUrl = buffer3.QueueDefinition.QueueUrl,
                                          MessageBody = GetNewId()
                                      }));
            3.Times(i => buffer3.Delete(new DeleteMessageRequest
                                        {
                                            QueueUrl = buffer3.QueueDefinition.QueueUrl,
                                            ReceiptHandle = GetNewId()
                                        }));
            2.Times(i => buffer3.ChangeVisibility(new ChangeMessageVisibilityRequest
                                                  {
                                                      QueueUrl = buffer3.QueueDefinition.QueueUrl,
                                                      ReceiptHandle = GetNewId()
                                                  }));

            // Should all have something buffered
            Assert.Greater(buffer.SendBufferCount, 0, "1 SendBufferCount");
            Assert.Greater(buffer.DeleteBufferCount, 0, "1 DeleteBufferCount");
            Assert.Greater(buffer.ChangeVisibilityBufferCount, 0, "1 CvBufferCount");

            Assert.Greater(buffer2.SendBufferCount, 0, "2 SendBufferCount");
            Assert.Greater(buffer2.DeleteBufferCount, 0, "2 DeleteBufferCount");
            Assert.Greater(buffer2.ChangeVisibilityBufferCount, 0, "2 CvBufferCount");

            Assert.Greater(buffer3.SendBufferCount, 0, "3 SendBufferCount");
            Assert.Greater(buffer3.DeleteBufferCount, 0, "4 DeleteBufferCount");
            Assert.Greater(buffer3.ChangeVisibilityBufferCount, 0, "3 CvBufferCount");
            
            // Set the buffer flush on the factory. Setting it back to zero will still have the timer fire
            // at least once, and then it will be cleared after the first fire.
            _sqsMqBufferFactory.BufferFlushIntervalSeconds = 1;
            _sqsMqBufferFactory.BufferFlushIntervalSeconds = 0;
            Thread.Sleep(1300);
            
            // Should all be drained
            Assert.AreEqual(buffer.SendBufferCount, 0, "1 SendBufferCount");
            Assert.AreEqual(buffer.DeleteBufferCount, 0, "1 DeleteBufferCount");
            Assert.AreEqual(buffer.ChangeVisibilityBufferCount, 0, "1 CvBufferCount");

            Assert.AreEqual(buffer2.SendBufferCount, 0, "2 SendBufferCount");
            Assert.AreEqual(buffer2.DeleteBufferCount, 0, "2 DeleteBufferCount");
            Assert.AreEqual(buffer2.ChangeVisibilityBufferCount, 0, "2 CvBufferCount");

            Assert.AreEqual(buffer3.SendBufferCount, 0, "3 SendBufferCount");
            Assert.AreEqual(buffer3.DeleteBufferCount, 0, "4 DeleteBufferCount");
            Assert.AreEqual(buffer3.ChangeVisibilityBufferCount, 0, "3 CvBufferCount");
        }

    }
}