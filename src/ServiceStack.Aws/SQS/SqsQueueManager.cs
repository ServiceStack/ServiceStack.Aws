using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Amazon.SQS;
using Amazon.SQS.Model;
using ServiceStack.Aws.Support;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack.Aws.SQS
{
    public class SqsQueueManager
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SqsQueueManager));
        private readonly ConcurrentDictionary<string, SqsQueueDefinition> _queueNameMap = new ConcurrentDictionary<string, SqsQueueDefinition>();

        private readonly SqsConnectionFactory _sqsConnectionFactory;
        private IAmazonSQS _sqsClient;
        
        public SqsQueueManager(SqsConnectionFactory sqsConnectionFactory)
        {
            Guard.AgainstNullArgument(sqsConnectionFactory, "sqsConnectionFactory");

            DefaultVisibilityTimeout = SqsQueueDefinition.DefaultVisibilityTimeoutSeconds;
            DefaultReceiveWaitTime = SqsQueueDefinition.DefaultWaitTimeSeconds;
            
            _sqsConnectionFactory = sqsConnectionFactory;
        }

        public int DefaultVisibilityTimeout { get; set; }
        public int DefaultReceiveWaitTime { get; set; }
        public bool DisableBuffering { get; set; }

        public SqsConnectionFactory ConnectionFactory
        {
            get { return _sqsConnectionFactory; }
        }

        public ConcurrentDictionary<string, SqsQueueDefinition> QueueNameMap
        {
            get { return _queueNameMap; }
        }

        private SqsQueueName GetSqsQueueName(string queueName)
        {
            SqsQueueDefinition qd;

            return _queueNameMap.TryGetValue(queueName, out qd)
                       ? qd.SqsQueueName
                       : SqsQueueNames.GetSqsQueueName(queueName);
        }

        public IAmazonSQS SqsClient
        {
            get { return _sqsClient ?? (_sqsClient = _sqsConnectionFactory.GetClient()); }
        }

        public Boolean QueueExists(string queueName, Boolean forceRecheck = false)
        {
            return QueueExists(GetSqsQueueName(queueName), forceRecheck);
        }

        private Boolean QueueExists(SqsQueueName queueName, Boolean forceRecheck = false)
        {
            if (!forceRecheck && _queueNameMap.ContainsKey(queueName.QueueName))
            {
                return true;
            }

            try
            {
                var definition = GetQueueDefinition(queueName, forceRecheck);
                return definition != null;
            }
            catch (QueueDoesNotExistException)
            {
                _log.DebugFormat("SQS Queue named [{0}] does not exist", queueName);
                return false;
            }
        }

        public String GetQueueUrl(string queueName, Boolean forceRecheck = false)
        {
            return GetQueueUrl(GetSqsQueueName(queueName), forceRecheck);
        }

        private String GetQueueUrl(SqsQueueName queueName, Boolean forceRecheck = false)
        {
            SqsQueueDefinition qd = null;

            if (!forceRecheck && _queueNameMap.TryGetValue(queueName.QueueName, out qd))
            {
                if (!String.IsNullOrEmpty(qd.QueueUrl))
                {
                    return qd.QueueUrl;
                }
            }

            var response = SqsClient.GetQueueUrl(queueName.AwsQueueName);
            return response.QueueUrl;
        }

        public SqsQueueDefinition GetQueueDefinition(string queueName, Boolean forceRecheck = false)
        {
            return GetQueueDefinition(GetSqsQueueName(queueName), forceRecheck);
        }

        private SqsQueueDefinition GetQueueDefinition(SqsQueueName queueName, Boolean forceRecheck = false)
        {
            SqsQueueDefinition qd = null;

            if (!forceRecheck && _queueNameMap.TryGetValue(queueName.QueueName, out qd))
            {
                return qd;
            }
            
            var queueUrl = GetQueueUrl(queueName);
            return GetQueueDefinition(queueName, queueUrl);
        }
        
        private SqsQueueDefinition GetQueueDefinition(string queueName, string queueUrl)
        {
            return GetQueueDefinition(GetSqsQueueName(queueName), queueUrl);
        }

        private SqsQueueDefinition GetQueueDefinition(SqsQueueName queueName, string queueUrl)
        {
            var response = SqsClient.GetQueueAttributes(new GetQueueAttributesRequest
                                                        {
                                                            QueueUrl = queueUrl,
                                                            AttributeNames = new List<string>
                                                                             {
                                                                                 "All"
                                                                             }
                                                        });

            var qd = response.Attributes.ToQueueDefinition(queueName, queueUrl, DisableBuffering);

            _queueNameMap[queueName.QueueName] = qd;

            return qd;
        }
        
        public SqsQueueDefinition GetOrCreate(string queueName, int? visibilityTimeoutSeconds = null,
                                              int? receiveWaitTimeSeconds = null, bool? disasbleBuffering = null)
        {
            SqsQueueDefinition qd = null;

            if (QueueExists(queueName) && _queueNameMap.TryGetValue(queueName, out qd))
            {
                return qd;
            }

            qd = CreateQueue(GetSqsQueueName(queueName), visibilityTimeoutSeconds,
                             receiveWaitTimeSeconds, disasbleBuffering);

            return qd;
        }

        public void DeleteQueue(string queueName)
        {
            try
            {
                var queueUrl = GetQueueUrl(queueName);
                DeleteQueue(queueName, queueUrl);
            }
            catch(QueueDoesNotExistException) { }
        }

        private void DeleteQueue(string queueName, string queueUrl)
        {
            DeleteQueue(GetSqsQueueName(queueName), queueUrl);
        }

        private void DeleteQueue(SqsQueueName queueName, string queueUrl)
        {
            var request = new DeleteQueueRequest
                          {
                              QueueUrl = queueUrl
                          };

            var response = SqsClient.DeleteQueue(request);

            SqsQueueDefinition qd;
            _queueNameMap.TryRemove(queueName.QueueName, out qd);
        }

        public SqsQueueDefinition CreateQueue(string queueName, SqsMqWorkerInfo info, string redriveArn = null)
        {
            var redrivePolicy = String.IsNullOrEmpty(redriveArn)
                                    ? null
                                    : new SqsRedrivePolicy
                                      {
                                          maxReceiveCount = info.RetryCount,
                                          deadLetterTargetArn = redriveArn
                                      };

            return CreateQueue(GetSqsQueueName(queueName), info.VisibilityTimeout, info.ReceiveWaitTime,
                               info.DisableBuffering, redrivePolicy);
        }

        public SqsQueueDefinition CreateQueue(string queueName, int? visibilityTimeoutSeconds = null,
                                              int? receiveWaitTimeSeconds = null, bool? disasbleBuffering = null,
                                              SqsRedrivePolicy redrivePolicy = null)
        {
            return CreateQueue(GetSqsQueueName(queueName), visibilityTimeoutSeconds, receiveWaitTimeSeconds,
                               disasbleBuffering, redrivePolicy);
        }

        private SqsQueueDefinition CreateQueue(SqsQueueName queueName, int? visibilityTimeoutSeconds = null,
                                               int? receiveWaitTimeSeconds = null, bool? disasbleBuffering = null,
                                               SqsRedrivePolicy redrivePolicy = null)
        {
            SqsQueueDefinition queueDefinition = null;

            var request = new CreateQueueRequest
                          {
                              QueueName = queueName.AwsQueueName,
                              Attributes = new Dictionary<string, string>
                                           {
                                               {
                                                   QueueAttributeName.ReceiveMessageWaitTimeSeconds,
                                                   TimeSpan.FromSeconds(receiveWaitTimeSeconds.HasValue
                                                                            ? receiveWaitTimeSeconds.Value
                                                                            : DefaultReceiveWaitTime)
                                                           .TotalSeconds
                                                           .ToString(CultureInfo.InvariantCulture)
                                               },
                                               {
                                                   QueueAttributeName.VisibilityTimeout,
                                                   TimeSpan.FromSeconds(visibilityTimeoutSeconds.HasValue
                                                                            ? visibilityTimeoutSeconds.Value
                                                                            : DefaultVisibilityTimeout)
                                                           .TotalSeconds
                                                           .ToString(CultureInfo.InvariantCulture)
                                               },
                                               {
                                                   QueueAttributeName.MessageRetentionPeriod,
                                                   (QueueNames.IsTempQueue(queueName.QueueName)
                                                        ? SqsQueueDefinition.DefaultTempQueueRetentionSeconds
                                                        : SqsQueueDefinition.DefaultPermanentQueueRetentionSeconds).ToString(CultureInfo.InvariantCulture)
                                               }
                                           }
                          };

            if (redrivePolicy != null)
            {
                request.Attributes.Add(QueueAttributeName.RedrivePolicy, redrivePolicy.ToJson());
            }

            try
            {
                var createResponse = SqsClient.CreateQueue(request);
                
                // Note - must go fetch the attributes from the server after creation, as the request attributes do not include
                // anything assigned by the server (i.e. the ARN, etc.).
                queueDefinition = GetQueueDefinition(queueName, createResponse.QueueUrl);
                
                queueDefinition.DisableBuffering = disasbleBuffering.HasValue
                                                       ? disasbleBuffering.Value
                                                       : DisableBuffering;

                _queueNameMap[queueDefinition.QueueName] = queueDefinition;
            }
            catch(QueueNameExistsException)
            {   // Queue exists with different attributes, instead of creating, alter those attributes to match what was requested
                queueDefinition = UpdateQueue(queueName, request.ToSetAttributesRequest(null), disasbleBuffering);
            }

            return queueDefinition;
        }

        private SqsQueueDefinition UpdateQueue(SqsQueueName sqsQueueName, SetQueueAttributesRequest request,
                                               bool? disasbleBuffering = null)
        {
            if (String.IsNullOrEmpty(request.QueueUrl))
            {
                request.QueueUrl = GetQueueUrl(sqsQueueName);
            }

            var response = SqsClient.SetQueueAttributes(request);

            // Note - must go fetch the attributes from the server after creation, as the request attributes do not include
            // anything assigned by the server (i.e. the ARN, etc.).
            var queueDefinition = GetQueueDefinition(sqsQueueName, request.QueueUrl);

            queueDefinition.DisableBuffering = disasbleBuffering.HasValue
                                                   ? disasbleBuffering.Value
                                                   : DisableBuffering;
            
            _queueNameMap[queueDefinition.QueueName] = queueDefinition;

            return queueDefinition;
        }

        public void PurgeQueue(string queueName)
        {
            try
            {
                PurgeQueueUrl(GetQueueUrl(queueName));
            }
            catch(QueueDoesNotExistException) { }
        }

        public void PurgeQueues(IEnumerable<string> queueNames)
        {
            foreach (var queueName in queueNames)
            {
                try
                {
                    var url = GetQueueUrl(queueName);
                    PurgeQueueUrl(url);
                }
                catch(QueueDoesNotExistException) { }
            }
        }

        private void PurgeQueueUrl(string queueUrl)
        {
            try
            {
                SqsClient.PurgeQueue(queueUrl);
            }
            catch(QueueDoesNotExistException) { }
            catch(PurgeQueueInProgressException) { }
        }
        
        public int RemoveEmptyTemporaryQueues(long createdBefore)
        {
            var queuesRemoved = 0;

            var localTempQueueUrlMap = new Dictionary<string, QueueNameUrlMap>();

            // First, check any locally available
            _queueNameMap.Where(kvp => QueueNames.IsTempQueue(kvp.Key))
                         .Where(kvp => kvp.Value.CreatedTimestamp <= createdBefore)
                         .Each(kvp => localTempQueueUrlMap.Add(kvp.Value.QueueUrl,
                                                               new QueueNameUrlMap
                                                               {
                                                                   QueueUrl = kvp.Value.QueueUrl,
                                                                   QueueName = kvp.Value.QueueName
                                                               }));

            // Refresh the local info for each of the potentials, then if they are empty and expired, remove
            foreach (var qNameUrl in localTempQueueUrlMap.Values)
            {
                var qd = GetQueueDefinition(qNameUrl.QueueName, qNameUrl.QueueUrl);

                if (qd.CreatedTimestamp > createdBefore || qd.ApproximateNumberOfMessages > 0)
                {
                    continue;
                }

                DeleteQueue(qd.SqsQueueName, qd.QueueUrl);
                queuesRemoved++;
            }

            var queues = SqsClient.ListQueues(new ListQueuesRequest
                                              {
                                                  QueueNamePrefix = QueueNames.TempMqPrefix.ToValidQueueName()
                                              });

            if (queues == null || queues.QueueUrls == null || queues.QueueUrls.Count <= 0)
            {
                return queuesRemoved;
            }

            foreach (var queueUrl in queues.QueueUrls)
            {
                if (localTempQueueUrlMap.ContainsKey(queueUrl))
                {
                    // Already deleted above, or left purposely
                    continue;
                }

                var response = SqsClient.GetQueueAttributes(new GetQueueAttributesRequest
                                                            {
                                                                QueueUrl = queueUrl,
                                                                AttributeNames = new List<string>
                                                                                 {
                                                                                     QueueAttributeName.CreatedTimestamp,
                                                                                     QueueAttributeName.ApproximateNumberOfMessages
                                                                                 }
                                                            });

                if (response == null || response.CreatedTimestamp.ToUnixTime() > createdBefore ||
                    response.ApproximateNumberOfMessages > 0)
                {
                    continue;
                }

                SqsClient.DeleteQueue(queueUrl);
                queuesRemoved++;
            }

            return queuesRemoved;
        }

        private class QueueNameUrlMap
        {
            public String QueueName { get; set; }
            public String QueueUrl { get; set; }
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
