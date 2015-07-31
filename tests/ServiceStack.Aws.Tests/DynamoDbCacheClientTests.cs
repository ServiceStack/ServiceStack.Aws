using Amazon.DynamoDBv2;
using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Server.Tests.Shared;

namespace ServiceStack.Aws.Tests
{
    [TestFixture]
    public class DynamoDbCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateClient()
        {
            {
                // Change App.config entry to url of DynamoDb server you have access to.
                // To setup a local dynamo instance see: https://aws.amazon.com/blogs/aws/dynamodb-local-for-desktop-development
                var config = new AmazonDynamoDBConfig
                {
                    ServiceURL = ConfigUtils.GetAppSetting("DynamoDbUrl", "http://localhost:8000")
                };

                var dynamoDbClient = new AmazonDynamoDBClient("keyId", "key", config);
                var cache = new DynamoDbCacheClient(dynamoDbClient);
                cache.InitSchema();
                return cache;
            }
        }
    }
}