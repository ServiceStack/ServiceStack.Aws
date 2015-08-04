using Amazon.DynamoDBv2;
using Amib.Threading;

namespace ServiceStack.Aws.DynamoDb
{
    public interface IPocoDynamoFactory
    {
        IPocoDynamo GetClient();
    }

    public class PocoDynamoFactory : IPocoDynamoFactory
    {
        private readonly Func<IAmazonDynamoDB> factory;

        public PocoDynamoFactory(Func<IAmazonDynamoDB> factory)
        {
            this.factory = factory;
        }

        public IPocoDynamo GetClient()
        {
            return new PocoDynamo(factory());
        }
    }
}