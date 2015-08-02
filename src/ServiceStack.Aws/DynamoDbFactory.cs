using Amazon;
using Amazon.DynamoDBv2;
using Amib.Threading;

namespace ServiceStack.Aws
{
    public static class DynamoDbFactory
    {
        public static Func<AmazonDynamoDBClient, IAmazonDynamoDB> CreateFilter { get; set; }

        private static IAmazonDynamoDB Create(AmazonDynamoDBClient client)
        {
            return CreateFilter != null
                ? CreateFilter(client)
                : client;
        }

        public static IAmazonDynamoDB Create(string url, string awsAccessKey = "keyId", string awsSecretKey = "key")
        {
            return Create(new AmazonDynamoDBClient(awsAccessKey, awsSecretKey, new AmazonDynamoDBConfig
            {
                ServiceURL = url,
            }));
        }

        public static IAmazonDynamoDB Create(RegionEndpoint endpoint, string awsAccessKey = "keyId", string awsSecretKey = "key")
        {
            return Create(new AmazonDynamoDBClient(awsAccessKey, awsSecretKey, endpoint));
        }
    }
}