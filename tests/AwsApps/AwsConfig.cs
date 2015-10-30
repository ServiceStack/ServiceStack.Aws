using System;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.S3;
using ServiceStack;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.S3;
using ServiceStack.Configuration;

namespace AwsApps
{
    public static class AwsConfig
    {
        public static string S3BucketName
        {
            get { return ConfigUtils.GetAppSetting("S3BucketName", "ss-awsdemo"); }
        }

        public static IAmazonDynamoDB CreateAmazonDynamoDb()
        {
            var dynamoClient = new AmazonDynamoDBClient(
                ConfigUtils.GetNullableAppSetting("DynamoDbAccessKey") ?? AwsAccessKey,
                ConfigUtils.GetNullableAppSetting("DynamoDbSecretKey") ?? AwsSecretKey,
                new AmazonDynamoDBConfig {
                    ServiceURL = ConfigUtils.GetAppSetting("DynamoDbUrl", "http://dynamodb.us-east-1.amazonaws.com"),
                });

            return dynamoClient;
        }

        public static IPocoDynamo CreatePocoDynamo()
        {
            return new PocoDynamo(CreateAmazonDynamoDb());
        }

        public static AmazonS3Client CreateAmazonS3Client()
        {
            return new AmazonS3Client(AwsAccessKey, AwsSecretKey, RegionEndpoint.USEast1);
        }

        public static S3VirtualPathProvider CreateS3VirtualPathProvider(IAppHost appHost, string bucketName)
        {
            return new S3VirtualPathProvider(CreateAmazonS3Client(), bucketName, appHost);
        }

        public static string AwsAccessKey
        {
            get
            {
                var accessKey = ConfigUtils.GetNullableAppSetting("AWS_ACCESS_KEY")
                    ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY");

                if (string.IsNullOrEmpty(accessKey))
                    throw new ArgumentException("AWS_ACCESS_KEY must be defined in App.config or Environment Variable");

                return accessKey;
            }
        }

        public static string AwsSecretKey
        {
            get
            {
                var secretKey = ConfigUtils.GetNullableAppSetting("AWS_SECRET_KEY")
                    ?? Environment.GetEnvironmentVariable("AWS_SECRET_KEY");

                if (string.IsNullOrEmpty(secretKey))
                    throw new ArgumentException("AWS_SECRET_KEY must be defined in App.config or Environment Variable");

                return secretKey;
            }
        }
    }
}