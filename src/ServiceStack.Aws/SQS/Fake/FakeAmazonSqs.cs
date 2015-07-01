﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace ServiceStack.Aws.SQS.Fake
{
    public class FakeAmazonSqs : IAmazonSQS
    {
        private static readonly FakeAmazonSqs _instance = new FakeAmazonSqs();
        private static readonly ConcurrentDictionary<string, FakeSqsQueue> _queues = new ConcurrentDictionary<string, FakeSqsQueue>();
        
        static FakeAmazonSqs() { }
        private FakeAmazonSqs() { }

        private FakeSqsQueue GetQueue(string queueUrl)
        {
            FakeSqsQueue q;

            if (!_queues.TryGetValue(queueUrl, out q))
            {
                throw new QueueDoesNotExistException("Queue does not exist for url [{0}]".Fmt(queueUrl));
            }

            return q;
        }

        public static FakeAmazonSqs Instance
        {
            get { return _instance; }
        }

        public void Dispose()
        {
            // Nothing to do here, this object is basically a singleton that mimics an SQS client with in-memory data backing,
            // so no disposal required (actually shouldn't do so even if you want to, as each time you dispose you'll wind up
            // clearning the "server" data)
        }
        
        public AddPermissionResponse AddPermission(string queueUrl, string label, List<string> awsAccountIds, List<string> actions)
        {
            throw new System.NotImplementedException();
        }

        public AddPermissionResponse AddPermission(AddPermissionRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<AddPermissionResponse> AddPermissionAsync(AddPermissionRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public ChangeMessageVisibilityResponse ChangeMessageVisibility(string queueUrl, string receiptHandle, int visibilityTimeout)
        {
            return ChangeMessageVisibility(new ChangeMessageVisibilityRequest
                                           {
                                               QueueUrl = queueUrl,
                                               ReceiptHandle = receiptHandle,
                                               VisibilityTimeout = visibilityTimeout
                                           });
        }

        public ChangeMessageVisibilityResponse ChangeMessageVisibility(ChangeMessageVisibilityRequest request)
        {
            var q = GetQueue(request.QueueUrl);

            var success = q.ChangeVisibility(request);

            return new ChangeMessageVisibilityResponse();
        }

        public Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(ChangeMessageVisibilityRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public ChangeMessageVisibilityBatchResponse ChangeMessageVisibilityBatch(string queueUrl, List<ChangeMessageVisibilityBatchRequestEntry> entries)
        {
            return ChangeMessageVisibilityBatch(new ChangeMessageVisibilityBatchRequest
                                                {
                                                    QueueUrl = queueUrl,
                                                    Entries = entries
                                                });
        }

        public ChangeMessageVisibilityBatchResponse ChangeMessageVisibilityBatch(ChangeMessageVisibilityBatchRequest request)
        {
            if (request.Entries == null || request.Entries.Count <= 0)
            {
                throw new EmptyBatchRequestException("No entires in request");
            }
            if (request.Entries.Count > SqsQueueDefinition.MaxBatchCvItems)
            {
                throw new TooManyEntriesInBatchRequestException("Count of [{0}] exceeds limit of [{1]}".Fmt(request.Entries.Count, SqsQueueDefinition.MaxBatchCvItems));
            }

            var q = GetQueue(request.QueueUrl);

            var response = new ChangeMessageVisibilityBatchResponse
                           {
                               Failed = new List<BatchResultErrorEntry>(),
                               Successful = new List<ChangeMessageVisibilityBatchResultEntry>()
                           };

            var entryIds = new HashSet<string>();

            foreach (var entry in request.Entries)
            {
                if (entryIds.Contains(entry.Id))
                {
                    throw new BatchEntryIdsNotDistinctException("Duplicate Id of [{0}]".Fmt(entry.Id));
                }

                entryIds.Add(entry.Id);

                var success = q.ChangeVisibility(new ChangeMessageVisibilityRequest
                                                 {
                                                     QueueUrl = request.QueueUrl,
                                                     ReceiptHandle = entry.ReceiptHandle,
                                                     VisibilityTimeout = entry.VisibilityTimeout
                                                 });

                if (success)
                {
                    response.Successful.Add(new ChangeMessageVisibilityBatchResultEntry
                                            {
                                                Id = entry.Id
                                            });
                }
                else
                {
                    response.Failed.Add(new BatchResultErrorEntry
                                        {
                                            Id = entry.Id,
                                            Message = "FakeCvError",
                                            Code = "123"
                                        });
                }
            }

            return response;
        }

        public Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(ChangeMessageVisibilityBatchRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public CreateQueueResponse CreateQueue(string queueName)
        {
            return CreateQueue(new CreateQueueRequest
                               {
                                   QueueName = queueName
                               });
        }

        public CreateQueueResponse CreateQueue(CreateQueueRequest request)
        {
            if (request.QueueName.Length > 80 ||
                request.QueueName.Any(c => !char.IsLetterOrDigit(c) && !SqsQueueDefinition.ValidNonAlphaNumericChars.Contains(c)))
            {
                throw new AmazonSQSException("Can only include alphanumeric characters, hyphens, or underscores. 1 to 80 in length");
            }

            var qUrl = Guid.NewGuid().ToString("N");

            var qd = request.Attributes.ToQueueDefinition(SqsQueueNames.GetSqsQueueName(request.QueueName), qUrl, disableBuffering: true);

            qd.QueueArn = Guid.NewGuid().ToString("N");
            
            var q = new FakeSqsQueue
                    {
                        QueueDefinition = qd
                    };

            if (_queues.Any(kvp => kvp.Value.QueueDefinition.QueueName.Equals(qd.QueueName, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new QueueNameExistsException("Queue with name [{0}] already exists".Fmt(qd.QueueName));
            }

            if (!_queues.TryAdd(qUrl, q))
            {
                throw new Exception("This should not happen, somehow the QueueUrl already exists");
            }
            
            return new CreateQueueResponse
                   {
                       QueueUrl = q.QueueDefinition.QueueUrl
                   };
        }

        public Task<CreateQueueResponse> CreateQueueAsync(CreateQueueRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public DeleteMessageResponse DeleteMessage(string queueUrl, string receiptHandle)
        {
            return DeleteMessage(new DeleteMessageRequest
                                 {
                                     QueueUrl = queueUrl,
                                     ReceiptHandle = receiptHandle
                                 });
        }

        public DeleteMessageResponse DeleteMessage(DeleteMessageRequest request)
        {
            var q = GetQueue(request.QueueUrl);

            var removed = q.DeleteMessage(request);

            return new DeleteMessageResponse();
        }

        public Task<DeleteMessageResponse> DeleteMessageAsync(DeleteMessageRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public DeleteMessageBatchResponse DeleteMessageBatch(string queueUrl, List<DeleteMessageBatchRequestEntry> entries)
        {
            return DeleteMessageBatch(new DeleteMessageBatchRequest
                                      {
                                          QueueUrl = queueUrl,
                                          Entries = entries
                                      });
        }

        public DeleteMessageBatchResponse DeleteMessageBatch(DeleteMessageBatchRequest request)
        {
            if (request.Entries == null || request.Entries.Count <= 0)
            {
                throw new EmptyBatchRequestException("No entires in request");
            }

            if (request.Entries.Count > SqsQueueDefinition.MaxBatchDeleteItems)
            {
                throw new TooManyEntriesInBatchRequestException("Count of [{0}] exceeds limit of [{1}]".Fmt(request.Entries.Count, SqsQueueDefinition.MaxBatchDeleteItems));
            }

            var q = GetQueue(request.QueueUrl);

            var response = new DeleteMessageBatchResponse
                           {
                               Failed = new List<BatchResultErrorEntry>(),
                               Successful = new List<DeleteMessageBatchResultEntry>()
                           };

            var entryIds = new HashSet<string>();

            foreach (var entry in request.Entries)
            {
                if (entryIds.Contains(entry.Id))
                {
                    throw new BatchEntryIdsNotDistinctException("Duplicate Id of [{0}]".Fmt(entry.Id));
                }

                entryIds.Add(entry.Id);

                var success = q.DeleteMessage(new DeleteMessageRequest
                                              {
                                                  QueueUrl = request.QueueUrl,
                                                  ReceiptHandle = entry.ReceiptHandle
                                              });

                if (success)
                {
                    response.Successful.Add(new DeleteMessageBatchResultEntry
                                            {
                                                Id = entry.Id
                                            });
                }
                else
                {
                    response.Failed.Add(new BatchResultErrorEntry
                                        {
                                            Id = entry.Id,
                                            Message = "FakeDeleteError",
                                            Code = "456"
                                        });
                }
            }

            return response;
        }

        public Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(DeleteMessageBatchRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public DeleteQueueResponse DeleteQueue(string queueUrl)
        {
            return DeleteQueue(new DeleteQueueRequest
                               {
                                   QueueUrl = queueUrl
                               });
        }

        public DeleteQueueResponse DeleteQueue(DeleteQueueRequest request)
        {
            FakeSqsQueue q;

            _queues.TryRemove(request.QueueUrl, out q);

            return new DeleteQueueResponse();
        }

        public Task<DeleteQueueResponse> DeleteQueueAsync(DeleteQueueRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public GetQueueAttributesResponse GetQueueAttributes(string queueUrl, List<string> attributeNames)
        {
            return GetQueueAttributes(new GetQueueAttributesRequest
                                      {
                                          QueueUrl = queueUrl,
                                          AttributeNames = attributeNames
                                      });
        }

        public GetQueueAttributesResponse GetQueueAttributes(GetQueueAttributesRequest request)
        {
            var q = GetQueue(request.QueueUrl);

            return new GetQueueAttributesResponse
                   {
                       Attributes = new Dictionary<string, string>
                                    {
                                        { QueueAttributeName.VisibilityTimeout, q.QueueDefinition.VisibilityTimeout.ToString(CultureInfo.InvariantCulture) },
                                        { QueueAttributeName.ReceiveMessageWaitTimeSeconds, q.QueueDefinition.ReceiveWaitTime.ToString(CultureInfo.InvariantCulture) },
                                        { QueueAttributeName.CreatedTimestamp, q.QueueDefinition.CreatedTimestamp.ToString(CultureInfo.InvariantCulture) },
                                        { QueueAttributeName.ApproximateNumberOfMessages, q.Count.ToString(CultureInfo.InvariantCulture) },
                                        { QueueAttributeName.QueueArn, q.QueueDefinition.QueueArn.ToString(CultureInfo.InvariantCulture) },
                                        { QueueAttributeName.RedrivePolicy, q.QueueDefinition.RedrivePolicy == null
                                                                                ? null
                                                                                : q.QueueDefinition.RedrivePolicy.ToJson() }
                                    }
                   };
        }

        public Task<GetQueueAttributesResponse> GetQueueAttributesAsync(GetQueueAttributesRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public GetQueueUrlResponse GetQueueUrl(string queueName)
        {
            return GetQueueUrl(new GetQueueUrlRequest
                               {
                                   QueueName = queueName
                               });
        }

        public GetQueueUrlResponse GetQueueUrl(GetQueueUrlRequest request)
        {
            var q = _queues.Where(kvp => kvp.Value.QueueDefinition.QueueName.Equals(request.QueueName, StringComparison.InvariantCultureIgnoreCase))
                           .Select(kvp => kvp.Value)
                           .SingleOrDefault();

            if (q == null)
            {
                throw new QueueDoesNotExistException("Queue does not exist with namel [{0}]".Fmt(request.QueueName));
            }

            return new GetQueueUrlResponse
                   {
                       QueueUrl = q.QueueDefinition.QueueUrl
                   };
        }

        public Task<GetQueueUrlResponse> GetQueueUrlAsync(GetQueueUrlRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public ListDeadLetterSourceQueuesResponse ListDeadLetterSourceQueues(ListDeadLetterSourceQueuesRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<ListDeadLetterSourceQueuesResponse> ListDeadLetterSourceQueuesAsync(ListDeadLetterSourceQueuesRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public ListQueuesResponse ListQueues(string queueNamePrefix)
        {
            return ListQueues(new ListQueuesRequest
                              {
                                  QueueNamePrefix = queueNamePrefix
                              });
        }

        public ListQueuesResponse ListQueues(ListQueuesRequest request)
        {
            var urls = String.IsNullOrEmpty(request.QueueNamePrefix)
                           ? _queues.Keys
                           : _queues.Values
                                    .Where(q => q.QueueDefinition
                                                 .QueueName
                                                 .StartsWith(request.QueueNamePrefix, StringComparison.InvariantCultureIgnoreCase))
                                    .Select(q => q.QueueDefinition.QueueUrl);

            var response = new ListQueuesResponse
                           {
                               QueueUrls = urls.ToList()
                           };

            return response;
        }

        public Task<ListQueuesResponse> ListQueuesAsync(ListQueuesRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public PurgeQueueResponse PurgeQueue(string queueUrl)
        {
            return PurgeQueue(new PurgeQueueRequest
                              {
                                  QueueUrl = queueUrl
                              });
        }

        public PurgeQueueResponse PurgeQueue(PurgeQueueRequest request)
        {
            var q = GetQueue(request.QueueUrl);
            q.Clear();
            return new PurgeQueueResponse();
        }

        public Task<PurgeQueueResponse> PurgeQueueAsync(PurgeQueueRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public ReceiveMessageResponse ReceiveMessage(string queueUrl)
        {
            return ReceiveMessage(new ReceiveMessageRequest
                                  {
                                      QueueUrl = queueUrl,
                                      MaxNumberOfMessages = 1,
                                      VisibilityTimeout = 30,
                                      WaitTimeSeconds = 0
                                  });
        }

        public ReceiveMessageResponse ReceiveMessage(ReceiveMessageRequest request)
        {
            if (request.MaxNumberOfMessages > SqsQueueDefinition.MaxBatchReceiveItems)
            {
                throw new TooManyEntriesInBatchRequestException("Count of [{0}] exceeds limit of [{1]}".Fmt(request.MaxNumberOfMessages, SqsQueueDefinition.MaxBatchReceiveItems));
            }

            var q = GetQueue(request.QueueUrl);

            var response = new ReceiveMessageResponse
                           {
                               Messages = q.Receive(request)
                                           .Select(qi => new Message
                                                         {
                                                             Body = qi.Body,
                                                             Attributes = qi.Attributes,
                                                             MessageId = qi.MessageId,
                                                             ReceiptHandle = qi.ReceiptHandle
                                                         })
                                           .ToList()
                           };

            return response;
        }

        public Task<ReceiveMessageResponse> ReceiveMessageAsync(ReceiveMessageRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public RemovePermissionResponse RemovePermission(string queueUrl, string label)
        {
            throw new System.NotImplementedException();
        }

        public RemovePermissionResponse RemovePermission(RemovePermissionRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<RemovePermissionResponse> RemovePermissionAsync(RemovePermissionRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public SendMessageResponse SendMessage(string queueUrl, string messageBody)
        {
            return SendMessage(new SendMessageRequest
                               {
                                   QueueUrl = queueUrl,
                                   MessageBody = messageBody,
                                   DelaySeconds = 0
                               });
        }

        public SendMessageResponse SendMessage(SendMessageRequest request)
        {
            var q = GetQueue(request.QueueUrl);

            var id = q.Send(request);

            return new SendMessageResponse
                   {
                       MessageId = id
                   };
        }

        public Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public SendMessageBatchResponse SendMessageBatch(string queueUrl, List<SendMessageBatchRequestEntry> entries)
        {
            return SendMessageBatch(new SendMessageBatchRequest
                                    {
                                        QueueUrl = queueUrl,
                                        Entries = entries
                                    });
        }

        public SendMessageBatchResponse SendMessageBatch(SendMessageBatchRequest request)
        {
            if (request.Entries == null || request.Entries.Count <= 0)
            {
                throw new EmptyBatchRequestException("No entires in request");
            }

            if (request.Entries.Count > SqsQueueDefinition.MaxBatchSendItems)
            {
                throw new TooManyEntriesInBatchRequestException("Count of [{0}] exceeds limit of [{1}]".Fmt(request.Entries.Count, SqsQueueDefinition.MaxBatchSendItems));
            }

            var q = GetQueue(request.QueueUrl);

            var response = new SendMessageBatchResponse
                           {
                               Failed = new List<BatchResultErrorEntry>(),
                               Successful = new List<SendMessageBatchResultEntry>()
                           };

            var entryIds = new HashSet<string>();

            foreach (var entry in request.Entries)
            {
                if (entryIds.Contains(entry.Id))
                {
                    throw new BatchEntryIdsNotDistinctException("Duplicate Id of [{0}]".Fmt(entry.Id));
                }

                entryIds.Add(entry.Id);

                var id = q.Send(new SendMessageRequest
                                {
                                    MessageBody = entry.MessageBody,
                                    QueueUrl = q.QueueDefinition.QueueUrl
                                });

                if (id == null)
                {
                    response.Failed.Add(new BatchResultErrorEntry
                                        {
                                            Id = entry.Id,
                                            Message = "FakeSendError",
                                            Code = "789"
                                        });
                }
                else
                {
                    response.Successful.Add(new SendMessageBatchResultEntry
                                            {
                                                Id = entry.Id,
                                                MessageId = id
                                            });
                }
            }

            return response;
        }

        public Task<SendMessageBatchResponse> SendMessageBatchAsync(SendMessageBatchRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public SetQueueAttributesResponse SetQueueAttributes(string queueUrl, Dictionary<string, string> attributes)
        {
            return SetQueueAttributes(new SetQueueAttributesRequest
                                      {
                                          QueueUrl = queueUrl,
                                          Attributes = attributes
                                      });
        }

        public SetQueueAttributesResponse SetQueueAttributes(SetQueueAttributesRequest request)
        {
            var q = GetQueue(request.QueueUrl);

            var qd = request.Attributes.ToQueueDefinition(SqsQueueNames.GetSqsQueueName(q.QueueDefinition.QueueName), q.QueueDefinition.QueueUrl, disableBuffering: true);

            if (qd.VisibilityTimeout > 0)
            {
                q.QueueDefinition.VisibilityTimeout = qd.VisibilityTimeout;
            }
            if (qd.ReceiveWaitTime > 0)
            {
                q.QueueDefinition.ReceiveWaitTime = qd.ReceiveWaitTime;
            }

            q.QueueDefinition.DisableBuffering = qd.DisableBuffering;
            q.QueueDefinition.RedrivePolicy = qd.RedrivePolicy;

            return new SetQueueAttributesResponse();
        }

        public Task<SetQueueAttributesResponse> SetQueueAttributesAsync(SetQueueAttributesRequest request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public string AuthorizeS3ToSendMessage(string queueUrl, string bucket)
        {
            throw new System.NotImplementedException();
        }
    }
}