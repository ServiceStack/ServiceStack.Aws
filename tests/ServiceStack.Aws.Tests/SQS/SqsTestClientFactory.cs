using Amazon;
using Amazon.SQS;
using ServiceStack.Aws.SQS;
using ServiceStack.Aws.SQS.Fake;

namespace ServiceStack.Aws.Tests.SQS
{
    public static class SqsTestClientFactory
    {
        public static IAmazonSQS GetClient()
        {
            return FakeAmazonSqs.Instance;
            //return new AmazonSQSClient("accessKeyId", "secretAccessKey", RegionEndpoint.USEast1);
        }

        public static SqsConnectionFactory GetConnectionFactory()
        {
            return new SqsConnectionFactory(GetClient);
        }

    }
}