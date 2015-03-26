using Amazon.DynamoDBv2;
using NUnit.Framework;
using ServiceStack.Caching;

namespace ServiceStack.Aws.Tests
{
    [TestFixture]
    public class DynamoDbCacheClientTests
    {
        private ICacheClient cacheClient;

        [TestFixtureSetUp]
        public void OnTestFixtureSetup()
        {
            var config = new AmazonDynamoDBConfig()
            {
                ServiceURL = "http://192.168.137.217:8000"
            };

            var dynamoDbClient = new AmazonDynamoDBClient("keyId", "key", config);
            this.cacheClient = new DynamoDbCacheClient(dynamoDbClient, createTableIfMissing: true);
        }

        [Test]
        public void Can_set_and_get()
        {
            this.cacheClient.Set("Car", "Audi");
            var response = this.cacheClient.Get<string>("Car");
            Assert.That(response, Is.EqualTo("Audi"));
        }
    }
}
