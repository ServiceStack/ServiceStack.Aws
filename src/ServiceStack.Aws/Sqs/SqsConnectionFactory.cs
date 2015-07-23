using System;
using Amazon;
using Amazon.SQS;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.Sqs
{
    public class SqsConnectionFactory
    {
        private readonly Func<IAmazonSQS> clientFactory;

        public SqsConnectionFactory() : this(() => new AmazonSQSClient()) { }

        public SqsConnectionFactory(string awsAccessKey, string awsSecretKey, RegionEndpoint region)
            : this(() => new AmazonSQSClient(awsAccessKey, awsSecretKey, region)) { }

        public SqsConnectionFactory(Func<IAmazonSQS> clientFactory)
        {
            Guard.AgainstNullArgument(clientFactory, "clientFactory");

            this.clientFactory = clientFactory;
        }

        public IAmazonSQS GetClient()
        {
            return clientFactory();
        }

    }
}