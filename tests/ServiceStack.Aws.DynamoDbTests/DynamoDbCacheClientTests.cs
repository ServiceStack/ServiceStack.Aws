using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Caching;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class DynamoDbCacheClientTests : CacheClientTestsBase
    {
        public override ICacheClient CreateCacheClient()
        {
            var cache = new DynamoDbCacheClient(DynamoTestBase.CreatePocoDynamo());
            cache.InitSchema();
            return cache;
        }
    }
}