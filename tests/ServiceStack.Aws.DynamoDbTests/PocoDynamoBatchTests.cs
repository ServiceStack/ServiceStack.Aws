using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class PocoDynamoBatchTests : DynamoTestBase
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
        }

        [Test]
        public void Can_GetAll()
        {
            var db = CreatePocoDynamo();
            var items = PutPocoItems(db);

            var results = db.GetAll<Poco>();

            Assert.That(results, Is.EquivalentTo(items));
        }

        [Test]
        public void Does_Batch_PutItems_and_GetItems()
        {
            var db = CreatePocoDynamo();
            var items = PutPocoItems(db);

            var results = db.GetItems<Poco>(items.Map(x => x.Id));

            Assert.That(results, Is.EquivalentTo(items));
        }

        [Test]
        public void Does_Batch_PutItems_and_GetItems_handles_multiple_batches()
        {
            var db = CreatePocoDynamo();
            var items = PutPocoItems(db, count: 110);

            var results = db.GetItems<Poco>(items.Map(x => x.Id));

            Assert.That(results, Is.EquivalentTo(items));
        }

        [Test]
        public void Does_Batch_DeleteItems()
        {
            var db = CreatePocoDynamo();
            var items = PutPocoItems(db, count: 20);

            var deleteIds = items.Take(10).Map(x => x.Id);

            db.DeleteItems<Poco>(deleteIds);

            var results = db.GetItems<Poco>(items.Map(x => x.Id));

            Assert.That(results.Count, Is.EqualTo(items.Count - deleteIds.Count));
        }
    }
}