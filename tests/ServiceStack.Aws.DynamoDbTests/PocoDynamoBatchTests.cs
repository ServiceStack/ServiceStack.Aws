using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Text;

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

        [Test]
        public void Can_select_just_field()
        {
            var db = CreatePocoDynamo();
            PutPocoItems(db, count: 30);

            var rows = db.FromScan<Poco>().Select(x => new { x.Id }).Exec().ToList();
            Assert.That(rows.All(x => x.Id != default(int)));
            Assert.That(rows.All(x => x.Title == null));

            var ids = db.FromScan<Poco>().ExecColumn(x => x.Id);
            Assert.That(ids.All(x => x != default(int)));
        }

        [Test]
        public void Does_get_ScanItemCount()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));

            var items = PutPocoItems(db, count: 20);

            var count = db.ScanItemCount<Poco>();
            Assert.That(count, Is.EqualTo(20));

            db.DeleteItems<Poco>(items.Take(10).Map(x => x.Id));

            count = db.ScanItemCount<Poco>();
            Assert.That(count, Is.EqualTo(10));

            db.DeleteItems<Poco>(items.Skip(10).Map(x => x.Id));

            count = db.ScanItemCount<Poco>();
            Assert.That(count, Is.EqualTo(0));
        }
    }
}