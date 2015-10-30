using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class PocoDynamoDbFilterExpressionTests : DynamoTestBase
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
        }

        [Test]
        public void Can_Scan_by_FilterExpression()
        {
            var db = CreatePocoDynamo();
            var items = PutPocoItems(db);

            var low5 = db.Scan<Poco>(q => q.Filter("Id < :Count", new Dictionary<string, object> { { "Count", 5 } }))
                .ToList();

            low5.PrintDump();

            var expected = items.Where(x => x.Id < 5).ToList();
            Assert.That(low5.Count, Is.EqualTo(5));
            Assert.That(low5, Is.EquivalentTo(expected));

            low5 = db.Scan<Poco>(q => q.Filter("Id < :Count", new { Count = 5 })).ToList();
            Assert.That(low5, Is.EquivalentTo(expected));

            low5 = db.Scan<Poco>(q => q.Filter(x => x.Id < 5)).ToList();
            Assert.That(low5, Is.EquivalentTo(expected));

            low5 = db.Scan(db.FromScan<Poco>(x => x.Id < 5)).ToList();
            Assert.That(low5, Is.EquivalentTo(expected));
        }

        [Test]
        public void Can_Scan_by_FilterExpression_with_Limit()
        {
            var db = CreatePocoDynamo();
            var items = PutPocoItems(db);

            var low5 = db.Scan<Poco>(q => q.Filter("Id < :Count", new Dictionary<string, object> { { "Count", 5 } }), limit: 5);

            low5.PrintDump();

            var expected = items.Where(x => x.Id < 5).ToList();
            Assert.That(low5.Count, Is.EqualTo(5));
            Assert.That(low5, Is.EquivalentTo(expected));

            low5 = db.Scan<Poco>(q => q.Filter("Id < :Count", new { Count = 5 }), limit: 5);
            Assert.That(low5, Is.EquivalentTo(expected));

            low5 = db.Scan(db.FromScan<Poco>(x => x.Id < 5), limit: 5);
            Assert.That(low5, Is.EquivalentTo(expected));

            var low3 = db.Scan<Poco>(q => q.Filter("Id < :Count", new { Count = 5 }), limit: 3);
            Assert.That(low3.Count, Is.EqualTo(3));

            low3 = db.Scan(db.FromScan<Poco>(x => x.Id < 5), limit: 3);
            Assert.That(low3.Count, Is.EqualTo(3));
        }

        [Test]
        public void Can_Scan_with_begins_with()
        {
            var db = CreatePocoDynamo();
            var items = PutPocoItems(db, count: 20);

            var expected = items.Where(x => x.Title.StartsWith("Name 1")).ToList();

            var results = db.Scan<Poco>(q => q.Filter("begins_with(Title, :s)", new { s = "Name 1" }));
            Assert.That(results, Is.EquivalentTo(expected));

            results = db.Scan(db.FromScan<Poco>(x => x.Title.StartsWith("Name 1")));
            Assert.That(results, Is.EquivalentTo(expected));
        }

        [Test]
        public void Can_Scan_with_contains_string()
        {
            var db = CreatePocoDynamo();
            var items = PutPocoItems(db, count: 20);

            var expected = items.Where(x => x.Title.Contains("ame 1")).ToList();

            var results = db.Scan<Poco>(q => q.Filter("contains(Title, :s)", new { s = "ame 1" }));
            Assert.That(results, Is.EquivalentTo(expected));

            results = db.Scan(db.FromScan<Poco>(x => x.Title.Contains("ame 1")));
            Assert.That(results, Is.EquivalentTo(expected));
        }

        [Test]
        public void Can_Scan_with_in()
        {
            var db = CreatePocoDynamo();
            var items = PutPocoItems(db);

            var names = new[] { "Name 1", "Name 2" };

            var expected = items.Where(x => names.Contains(x.Title)).ToList();

            var results = db.Scan(db.FromScan<Poco>(x => names.Contains(x.Title)));
            Assert.That(results, Is.EquivalentTo(expected));
        }

        [Test]
        public void Can_check_for_null()
        {
            var db = CreatePocoDynamo();

            db.RegisterTable<Poco>();
            db.InitSchema();

            db.PutItem(new Poco { Id = 1, Title = "Has Value" });
            db.PutItem(new Poco { Id = 2, Title = null });

            var request = new ScanRequest
            {
                TableName = db.GetTableMetadata(typeof(Poco)).Name,
                FilterExpression = "Title = :null",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":null", new AttributeValue { NULL = true } },
                }
            };

            var results = db.Scan<Poco>(request).ToList();
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Title, Is.Null);

            results = db.FromScan<Poco>()
                .Filter(x => x.Title == null)
                .Exec()
                .ToList();
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Title, Is.Null);
        }

        [Test]
        public void Can_check_for_not_null()
        {
            var db = CreatePocoDynamo();

            db.RegisterTable<Poco>();
            db.InitSchema();

            db.PutItem(new Poco { Id = 1, Title = "Has Value" });
            db.PutItem(new Poco { Id = 2, Title = null });

            var request = new ScanRequest
            {
                TableName = db.GetTableMetadata(typeof(Poco)).Name,
                FilterExpression = "Title <> :null",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":null", new AttributeValue { NULL = true } },
                }
            };

            var results = db.Scan<Poco>(request).ToList();
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Title, Is.EqualTo("Has Value"));

            results = db.FromScan<Poco>()
                .Filter(x => x.Title != null)
                .Exec()
                .ToList();
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Title, Is.EqualTo("Has Value"));
        }
    }
}