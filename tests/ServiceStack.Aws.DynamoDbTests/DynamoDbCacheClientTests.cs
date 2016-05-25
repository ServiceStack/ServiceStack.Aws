using System;
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

        [Test]
        public void Can_delete_expired_items()
        {
            var cache = (DynamoDbCacheClient)CreateCacheClient();
            cache.RemoveAll(cache.Dynamo.FromScan<CacheEntry>().ExecColumn(x => x.Id));

            cache.Add("expired1h", "expired", DateTime.UtcNow.AddHours(-1));
            cache.Add("expired1m", "expired", DateTime.UtcNow.AddMinutes(-1));
            cache.Add("valid1m", "valid", DateTime.UtcNow.AddMinutes(1));
            cache.Add("valid1h", "valid", DateTime.UtcNow.AddHours(1));

            cache.ClearExpiredEntries();

            var validEntries = cache.Dynamo.ScanAll<CacheEntry>().Map(x => x.Id);
            Assert.That(validEntries, Is.EquivalentTo(new[] { "valid1m", "valid1h" }));
        }
    }
}