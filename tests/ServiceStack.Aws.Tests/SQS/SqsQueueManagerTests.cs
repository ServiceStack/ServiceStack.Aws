using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.SQS.Model;
using NUnit.Framework;
using ServiceStack.Aws.SQS;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.SQS
{
    [TestFixture]
    public class SqsQueueManagerTests
    {
        private SqsQueueManager _sqsQueueManager;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            _sqsQueueManager = new SqsQueueManager(SqsTestClientFactory.GetConnectionFactory());
        }

        [TestFixtureTearDown]
        public void FixtureTeardown()
        {
            if (SqsTestAssert.IsFakeClient)
            {
                return;
            }
            
            // Cleanup anything left cached that we tested with
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

        private string GetNewId()
        {
            return Guid.NewGuid().ToString("N");
        }

        [Test]
        public void Queue_exists_returns_false_for_non_existent_queue()
        {
            Assert.IsFalse(_sqsQueueManager.QueueExists(GetNewId()));
        }

        [Test]
        public void Queue_exists_returns_false_for_non_existent_queue_that_is_already_cached_when_forced()
        {
            var qd = _sqsQueueManager.CreateQueue(GetNewId());

            Assert.IsTrue(_sqsQueueManager.QueueExists(qd.QueueName));

            // delete q directly at the client
            Assert.IsNotNull(_sqsQueueManager.SqsClient.DeleteQueue(qd.QueueUrl));

            // should still be in the cache
            Assert.IsTrue(_sqsQueueManager.QueueNameMap.ContainsKey(qd.QueueName));

            // should still return true when not forced, false when forced
            Assert.IsTrue(_sqsQueueManager.QueueExists(qd.QueueName));
            Assert.IsFalse(_sqsQueueManager.QueueExists(qd.QueueName, forceRecheck: true));
        }

        [Test]
        public void Queue_exists_returns_true_for_existent_queue()
        {
            var qd = _sqsQueueManager.CreateQueue(GetNewId());

            Assert.IsTrue(_sqsQueueManager.QueueExists(qd.QueueName));
        }

        [Test]
        public void Queue_exists_returns_true_for_existent_queue_that_is_not_cached()
        {
            var qd = _sqsQueueManager.CreateQueue(GetNewId());

            Assert.IsTrue(_sqsQueueManager.QueueExists(qd.QueueName));

            // Remove from the cache in the manager
            SqsQueueDefinition removedQd;
            Assert.IsTrue(_sqsQueueManager.QueueNameMap.TryRemove(qd.QueueName, out removedQd));
            Assert.IsTrue(ReferenceEquals(qd, removedQd));

            // Should still show exists without being forced
            Assert.IsTrue(_sqsQueueManager.QueueExists(qd.QueueName));
        }

        [Test]
        public void Queue_url_returns_for_existent_queue()
        {
            var qd = _sqsQueueManager.CreateQueue(GetNewId());

            Assert.IsNotNullOrEmpty(_sqsQueueManager.GetQueueUrl(qd.QueueName));
        }

        [Test]
        public void Queue_url_returns_for_existent_queue_that_is_not_cached()
        {
            var qd = _sqsQueueManager.CreateQueue(GetNewId());

            var url = _sqsQueueManager.GetQueueUrl(qd.QueueName);

            Assert.AreEqual(qd.QueueUrl, url, "QueueUrl not equal");

            // Remove from the cache in the manager
            SqsQueueDefinition removedQd;
            Assert.IsTrue(_sqsQueueManager.QueueNameMap.TryRemove(qd.QueueName, out removedQd));
            Assert.IsTrue(ReferenceEquals(qd, removedQd));

            Assert.IsNotNullOrEmpty(_sqsQueueManager.GetQueueUrl(qd.QueueName));
        }

        [Test]
        public void Queue_url_does_not_return_for_non_existent_queue()
        {
            Assert.Throws<QueueDoesNotExistException>(() => _sqsQueueManager.GetQueueUrl(GetNewId()));
        }

        [Test]
        public void Queue_url_does_not_return_for_non_existent_queue_that_is_already_cached_when_forced()
        {
            var qd = _sqsQueueManager.CreateQueue(GetNewId());

            var url = _sqsQueueManager.GetQueueUrl(qd.QueueName);

            Assert.AreEqual(qd.QueueUrl, url, "QueueUrl not equal");

            // delete q directly at the client
            Assert.IsNotNull(_sqsQueueManager.SqsClient.DeleteQueue(qd.QueueUrl));

            // should still be in the cache
            Assert.IsTrue(_sqsQueueManager.QueueNameMap.ContainsKey(qd.QueueName));

            // should still return true when not forced, false when forced
            Assert.IsNotNullOrEmpty(_sqsQueueManager.GetQueueUrl(qd.QueueName));
            Assert.Throws<QueueDoesNotExistException>(() => _sqsQueueManager.GetQueueUrl(qd.QueueName, forceRecheck: true));
        }

        [Test]
        public void Qd_returns_for_existent_queue()
        {
            var qd = _sqsQueueManager.CreateQueue(GetNewId());

            var returnedQd = _sqsQueueManager.GetQueueDefinition(qd.QueueName);

            Assert.IsTrue(ReferenceEquals(qd, returnedQd));
        }

        [Test]
        public void Qd_returns_for_existent_queue_that_is_not_cached()
        {
            var qd = _sqsQueueManager.CreateQueue(GetNewId());

            var returnedQd = _sqsQueueManager.GetQueueDefinition(qd.QueueName);

            Assert.IsTrue(ReferenceEquals(qd, returnedQd));

            // Remove from the cache in the manager
            SqsQueueDefinition removedQd;
            Assert.IsTrue(_sqsQueueManager.QueueNameMap.TryRemove(qd.QueueName, out removedQd));
            Assert.IsTrue(ReferenceEquals(qd, removedQd));

            var newQd = _sqsQueueManager.GetQueueDefinition(qd.QueueName);

            Assert.IsNotNull(newQd);
            Assert.AreEqual(qd.QueueUrl, newQd.QueueUrl, "QueueUrl");
            Assert.AreEqual(qd.QueueName, newQd.QueueName, "QueueName");
            Assert.AreEqual(qd.QueueArn, newQd.QueueArn, "QueueArn");
        }

        [Test]
        public void Qd_does_not_return_for_non_existent_queue()
        {
            Assert.Throws<QueueDoesNotExistException>(() => _sqsQueueManager.GetQueueDefinition(GetNewId()));
        }

        [Test]
        public void Qd_does_not_return_for_non_existent_queue_that_is_already_cached_when_forced()
        {
            var qd = _sqsQueueManager.CreateQueue(GetNewId());

            var returnedQd = _sqsQueueManager.GetQueueDefinition(qd.QueueName);

            Assert.IsTrue(ReferenceEquals(qd, returnedQd));

            // delete q directly at the client
            Assert.IsNotNull(_sqsQueueManager.SqsClient.DeleteQueue(qd.QueueUrl));

            // should still be in the cache
            Assert.IsTrue(_sqsQueueManager.QueueNameMap.ContainsKey(qd.QueueName));

            // should still return true when not forced, false when forced
            Assert.IsNotNull(_sqsQueueManager.GetQueueDefinition(qd.QueueName));

            SqsTestAssert.Throws<QueueDoesNotExistException>(() => _sqsQueueManager.GetQueueDefinition(qd.QueueName, forceRecheck: true), "specified queue does not exist");
        }
        
        [Test]
        public void GetOrCreate_creates_when_does_not_exist()
        {
            var name = GetNewId();
            var qd = _sqsQueueManager.GetOrCreate(name);

            Assert.IsNotNull(qd);
            Assert.AreEqual(qd.QueueName, name);
        }

        [Test]
        public void GetOrCreate_gets_when_exists()
        {
            var name = GetNewId();
            var qd = _sqsQueueManager.GetOrCreate(name);

            Assert.IsNotNull(qd);
            Assert.AreEqual(qd.QueueName, name);

            var getQd = _sqsQueueManager.GetOrCreate(name);

            Assert.IsTrue(ReferenceEquals(qd, getQd));
        }

        [Test]
        public void Delete_fails_quietly_when_queue_does_not_exist()
        {
            Assert.DoesNotThrow(() => _sqsQueueManager.DeleteQueue(GetNewId()));
        }

        [Test]
        public void Delete_succeeds_on_existing_queue()
        {
            var name = GetNewId();
            var qd = _sqsQueueManager.CreateQueue(name);

            Assert.IsNotNull(qd);
            Assert.AreEqual(name, qd.QueueName);

            Assert.IsTrue(_sqsQueueManager.QueueExists(qd.QueueName));

            Assert.DoesNotThrow(() => _sqsQueueManager.DeleteQueue(qd.QueueName));
            Assert.IsFalse(_sqsQueueManager.QueueExists(qd.QueueName));
        }

        [Test]
        public void Create_includes_correct_info_when_created_from_worker()
        {
            var info = new SqsMqWorkerInfo
                       {
                           VisibilityTimeout = 11,
                           ReceiveWaitTime = 9,
                           DisableBuffering = true,
                           RetryCount = 6
                       };

            var redriveQd = _sqsQueueManager.CreateQueue(GetNewId(), info);

            var qd = _sqsQueueManager.CreateQueue(GetNewId(), info, redriveArn: redriveQd.QueueArn);

            Assert.IsNotNull(qd, "Queue Definition");
            Assert.AreEqual(qd.VisibilityTimeout, info.VisibilityTimeout, "VisibilityTimeout");
            Assert.AreEqual(qd.ReceiveWaitTime, info.ReceiveWaitTime, "ReceiveWaitTime");
            Assert.AreEqual(qd.DisableBuffering, info.DisableBuffering, "DisableBuffering");

            Assert.IsNotNull(qd.RedrivePolicy, "RedrivePolicy");
            Assert.AreEqual(qd.RedrivePolicy.maxReceiveCount, info.RetryCount, "RetryCount");
            Assert.AreEqual(qd.RedrivePolicy.deadLetterTargetArn, redriveQd.QueueArn, "Redrive TargetArn");

        }

        [Test]
        public void Create_updates_existing_queue_when_created_with_different_attributes()
        {
            var info = new SqsMqWorkerInfo
                       {
                           VisibilityTimeout = 11,
                           ReceiveWaitTime = 9,
                           DisableBuffering = true,
                           RetryCount = 6
                       };

            var redriveQd = _sqsQueueManager.CreateQueue(GetNewId(), info);

            var qd = _sqsQueueManager.CreateQueue(GetNewId(), info, redriveArn: redriveQd.QueueArn);

            Assert.IsNotNull(qd, "First Queue Definition");
            Assert.IsTrue(_sqsQueueManager.QueueExists(qd.QueueName), "First Queue");

            var newRedriveQd = _sqsQueueManager.CreateQueue(GetNewId(), info);
            
            var newQd = _sqsQueueManager.CreateQueue(qd.QueueName, visibilityTimeoutSeconds: 12,
                                                     receiveWaitTimeSeconds: 10, disasbleBuffering: false,
                                                     redrivePolicy: new SqsRedrivePolicy
                                                                    {
                                                                        deadLetterTargetArn = newRedriveQd.QueueArn,
                                                                        maxReceiveCount = 7
                                                                    });

            Assert.IsNotNull(newQd, "New Queue Definition");
            Assert.AreEqual(newQd.VisibilityTimeout, 12, "VisibilityTimeout");
            Assert.AreEqual(newQd.ReceiveWaitTime, 10, "ReceiveWaitTime");
            Assert.AreEqual(newQd.DisableBuffering, false, "DisableBuffering");
            Assert.IsNotNull(newQd.RedrivePolicy, "RedrivePolicy");
            Assert.AreEqual(newQd.RedrivePolicy.maxReceiveCount, 7, "RetryCount");
            Assert.AreEqual(newQd.RedrivePolicy.deadLetterTargetArn, newRedriveQd.QueueArn, "Redrive TargetArn");

        }

        [Test]
        public void Purge_fails_quietly_when_queue_does_not_exist()
        {
            Assert.DoesNotThrow(() => _sqsQueueManager.PurgeQueue(GetNewId()));
        }

        [Test]
        public void Can_remove_empty_temp_queues()
        {
            var nonEmptyTempQueue = _sqsQueueManager.CreateQueue(QueueNames.GetTempQueueName());

            _sqsQueueManager.SqsClient.SendMessage(nonEmptyTempQueue.QueueUrl, "Just some text");
            _sqsQueueManager.SqsClient.SendMessage(nonEmptyTempQueue.QueueUrl, "Just some more text");

            var emptyTempQueue1 = _sqsQueueManager.CreateQueue(QueueNames.GetTempQueueName());
            var emptyTempQueue2 = _sqsQueueManager.CreateQueue(QueueNames.GetTempQueueName());
            var emptyTempQueueNotCached = _sqsQueueManager.CreateQueue(QueueNames.GetTempQueueName());

            if (!SqsTestAssert.IsFakeClient)
            {   // List queue doesn't return newly created queues for a bit, so if this a "real", we skip this part
                SqsQueueDefinition qd;
                _sqsQueueManager.QueueNameMap.TryRemove(emptyTempQueueNotCached.QueueName, out qd);
            }

            var countOfQueuesRemoved = _sqsQueueManager.RemoveEmptyTemporaryQueues(DateTime.UtcNow.AddDays(5).ToUnixTime());

            try
            {
                SqsTestAssert.FakeEqualRealGreater(3, 2, countOfQueuesRemoved);
            }
            finally
            {
                // Cleanup
                _sqsQueueManager.DeleteQueue(nonEmptyTempQueue.QueueName);
                _sqsQueueManager.DeleteQueue(emptyTempQueue1.QueueName);
                _sqsQueueManager.DeleteQueue(emptyTempQueue2.QueueName);
                _sqsQueueManager.DeleteQueue(emptyTempQueueNotCached.QueueName);
            }
        }

    }
}