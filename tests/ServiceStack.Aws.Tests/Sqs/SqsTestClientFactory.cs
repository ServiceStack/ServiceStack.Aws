using Amazon.SQS;
using ServiceStack.Aws.Sqs;
using ServiceStack.Aws.Sqs.Fake;

namespace ServiceStack.Aws.Tests.Sqs
{
    public static class SqsTestClientFactory
    {
        public static IAmazonSQS GetClient()
        {   // To test with a Fake instance, use the first line, for a real SQS instance, add appropriate creds
            // in second line and swap it as being what is returned vs. the fake. This applies to all tests.
            
            return FakeAmazonSqs.Instance;
            
            //return new AmazonSQSClient("accessKeyId", "secretAccessKey", RegionEndpoint.USEast1);
        }

        public static SqsConnectionFactory GetConnectionFactory()
        {
            return new SqsConnectionFactory(GetClient);
        }

    }
}