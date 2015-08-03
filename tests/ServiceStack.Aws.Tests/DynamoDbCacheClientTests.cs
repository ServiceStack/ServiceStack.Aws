using NUnit.Framework;
using ServiceStack.Caching;
using ServiceStack.Server.Tests.Shared;

namespace ServiceStack.Aws.Tests
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