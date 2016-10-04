#if NETSTANDARD1_6

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace ServiceStack.Aws
{
    internal static class NetCoreExtensions
    {
        internal static DescribeTableResponse DescribeTable(this IAmazonDynamoDB client, DescribeTableRequest request)
        {
            return client.DescribeTableAsync(request).Result;
        }

        internal static ListTablesResponse ListTables(this IAmazonDynamoDB client, ListTablesRequest request)
        {
            return client.ListTablesAsync(request).Result;
        }

        internal static CreateTableResponse CreateTable(this IAmazonDynamoDB client, CreateTableRequest request)
        {
            return client.CreateTableAsync(request).Result;
        }

        internal static DeleteTableResponse DeleteTable(this IAmazonDynamoDB client, DeleteTableRequest request)
        {
            return client.DeleteTableAsync(request).Result;
        }

        internal static GetItemResponse GetItem(this IAmazonDynamoDB client, GetItemRequest request)
        {
            return client.GetItemAsync(request).Result;
        }

        internal static PutItemResponse PutItem(this IAmazonDynamoDB client, PutItemRequest request)
        {
            return client.PutItemAsync(request).Result;
        }

        internal static UpdateItemResponse UpdateItem(this IAmazonDynamoDB client, UpdateItemRequest request)
        {
            return client.UpdateItemAsync(request).Result;
        }

        internal static DeleteItemResponse DeleteItem(this IAmazonDynamoDB client, DeleteItemRequest request)
        {
            return client.DeleteItemAsync(request).Result;
        }

        internal static BatchGetItemResponse BatchGetItem(this IAmazonDynamoDB client, BatchGetItemRequest request)
        {
            return client.BatchGetItemAsync(request).Result;
        }

        internal static BatchWriteItemResponse BatchWriteItem(this IAmazonDynamoDB client, BatchWriteItemRequest request)
        {
            return client.BatchWriteItemAsync(request).Result;
        }

        internal static ScanResponse Scan(this IAmazonDynamoDB client, ScanRequest request)
        {
            return client.ScanAsync(request).Result;
        }

        internal static QueryResponse Query(this IAmazonDynamoDB client, QueryRequest request)
        {
            return client.QueryAsync(request).Result;
        }

        internal static GetObjectMetadataResponse GetObjectMetadata(this IAmazonS3 client, GetObjectMetadataRequest request)
        {
            return client.GetObjectMetadataAsync(request).Result;
        }

        internal static GetObjectResponse GetObject(this IAmazonS3 client, GetObjectRequest request)
        {
            return client.GetObjectAsync(request).Result;
        }

        internal static PutObjectResponse PutObject(this IAmazonS3 client, PutObjectRequest request)
        {
            return client.PutObjectAsync(request).Result;
        }

        internal static DeleteObjectResponse DeleteObject(this IAmazonS3 client, DeleteObjectRequest request)
        {
            return client.DeleteObjectAsync(request).Result;
        }

        internal static DeleteObjectsResponse DeleteObjects(this IAmazonS3 client, DeleteObjectsRequest request)
        {
            return client.DeleteObjectsAsync(request).Result;
        }

        internal static CopyObjectResponse CopyObject(this IAmazonS3 client, CopyObjectRequest request)
        {
            return client.CopyObjectAsync(request).Result;
        }

        internal static ListObjectsResponse ListObjects(this IAmazonS3 client, ListObjectsRequest request)
        {
            return client.ListObjectsAsync(request).Result;
        }

        internal static GetBucketLocationResponse GetBucketLocation(this IAmazonS3 client, GetBucketLocationRequest request)
        {
            return client.GetBucketLocationAsync(request).Result;
        }

        internal static DeleteBucketResponse DeleteBucket(this IAmazonS3 client, DeleteBucketRequest request)
        {
            return client.DeleteBucketAsync(request).Result;
        }

        internal static PutBucketResponse PutBucket(this IAmazonS3 client, PutBucketRequest request)
        {
            return client.PutBucketAsync(request).Result;
        }

        internal static GetQueueUrlResponse GetQueueUrl(this IAmazonSQS client, GetQueueUrlRequest request)
        {
            return client.GetQueueUrlAsync(request).Result;
        }

        internal static GetQueueAttributesResponse GetQueueAttributes(this IAmazonSQS client, GetQueueAttributesRequest request)
        {
            return client.GetQueueAttributesAsync(request).Result;
        }

        internal static SetQueueAttributesResponse SetQueueAttributes(this IAmazonSQS client, SetQueueAttributesRequest request)
        {
            return client.SetQueueAttributesAsync(request).Result;
        }

        internal static CreateQueueResponse CreateQueue(this IAmazonSQS client, CreateQueueRequest request)
        {
            return client.CreateQueueAsync(request).Result;
        }

        internal static DeleteQueueResponse DeleteQueue(this IAmazonSQS client, DeleteQueueRequest request)
        {
            return client.DeleteQueueAsync(request).Result;
        }

        internal static ListQueuesResponse ListQueues(this IAmazonSQS client, ListQueuesRequest request)
        {
            return client.ListQueuesAsync(request).Result;
        }

        internal static PurgeQueueResponse PurgeQueue(this IAmazonSQS client, PurgeQueueRequest request)
        {
            return client.PurgeQueueAsync(request).Result;
        }

        internal static SendMessageResponse SendMessage(this IAmazonSQS client, SendMessageRequest request)
        {
            return client.SendMessageAsync(request).Result;
        }

        internal static SendMessageBatchResponse SendMessageBatch(this IAmazonSQS client, SendMessageBatchRequest request)
        {
            return client.SendMessageBatchAsync(request).Result;
        }

        internal static ReceiveMessageResponse ReceiveMessage(this IAmazonSQS client, ReceiveMessageRequest request)
        {
            return client.ReceiveMessageAsync(request).Result;
        }

        internal static DeleteMessageResponse DeleteMessage(this IAmazonSQS client, DeleteMessageRequest request)
        {
            return client.DeleteMessageAsync(request).Result;
        }

        internal static DeleteMessageBatchResponse DeleteMessageBatch(this IAmazonSQS client, DeleteMessageBatchRequest request)
        {
            return client.DeleteMessageBatchAsync(request).Result;
        }

        internal static ChangeMessageVisibilityBatchResponse ChangeMessageVisibilityBatch(this IAmazonSQS client, ChangeMessageVisibilityBatchRequest request)
        {
            return client.ChangeMessageVisibilityBatchAsync(request).Result;
        }

        internal static ChangeMessageVisibilityResponse ChangeMessageVisibility(this IAmazonSQS client, ChangeMessageVisibilityRequest request)
        {
            return client.ChangeMessageVisibilityAsync(request).Result;
        }

        internal static void WriteResponseStreamToFile(this GetObjectResponse response, string filePath, bool append)
        {
            response.WriteResponseStreamToFileAsync(filePath, append, default(CancellationToken)).Wait();
        }
    }
}

#endif