using System;
using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Server.Tests.Shared;

namespace ServiceStack.Aws.Tests
{
    public abstract class DynamoTestBase
    {
        public static bool CreateTestTables(IPocoDynamo db)
        {
            var types = new List<Type>()
                .Add<Customer>()
                .Add<CustomerAddress>()
                .Add<Order>()
                .Add<Country>()
                .Add<Node>();

            var tables = db.RegisterTables(types);
            var allTablesCreated = db.CreateMissingTables(tables, TimeSpan.FromMinutes(1));
            return allTablesCreated;
        }

        public static IPocoDynamo CreatePocoDynamo()
        {
            var dynamoClient = CreateDynamoDbClient();

            var db = new PocoDynamo(dynamoClient);
            return db;
        }

        public static ICacheClient CreateCacheClient()
        {
            var cache = new DynamoDbCacheClient(CreatePocoDynamo());
            cache.InitSchema();
            return cache;
        }

        public static AmazonDynamoDBClient CreateDynamoDbClient()
        {
            var accessKey = Environment.GetEnvironmentVariable("AWSAccessKey");
            var secretKey = Environment.GetEnvironmentVariable("AWSSecretKey");

            var useLocalDb = string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey);

            var dynamoClient = useLocalDb
                ? new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig {
                    ServiceURL = ConfigUtils.GetAppSetting("DynamoDbUrl", "http://localhost:8000"),
                })
                : new AmazonDynamoDBClient(accessKey, secretKey, RegionEndpoint.USEast1);
            return dynamoClient;
        }
    }
}