using System;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;

namespace ServiceStack.Aws.Tests.PocoDynamoTests
{
    public class SequenceGeneratorTests : DynamoTestBase
    {
        [Test]
        public void Can_increment_Seq()
        {
            var db = CreatePocoDynamo();

            db.CreateTableIfMissing(db.RegisterTable<Seq>());

            var key = Guid.NewGuid().ToString();

            var nextId = db.IncrementById<Seq>(key, "Counter");
            Assert.That(nextId, Is.EqualTo(1));

            nextId = db.IncrementById<Seq>(key, "Counter");
            Assert.That(nextId, Is.EqualTo(2));

            nextId = db.IncrementById<Seq>(key, "Counter", 10);
            Assert.That(nextId, Is.EqualTo(12));

            nextId = db.IncrementById<Seq>(key, "Counter", 10);
            Assert.That(nextId, Is.EqualTo(22));
        }

        [Test]
        public void Can_decrement_Seq()
        {
            var db = CreatePocoDynamo();

            db.CreateTableIfMissing(db.RegisterTable<Seq>());

            var key = Guid.NewGuid().ToString();

            var nextId = db.DecrementById<Seq>(key, "Counter");
            Assert.That(nextId, Is.EqualTo(-1));

            nextId = db.DecrementById<Seq>(key, "Counter");
            Assert.That(nextId, Is.EqualTo(-2));

            nextId = db.DecrementById<Seq>(key, "Counter", 10);
            Assert.That(nextId, Is.EqualTo(-12));

            nextId = db.DecrementById<Seq>(key, "Counter", 10);
            Assert.That(nextId, Is.EqualTo(-22));
        }

        [Test]
        public void Can_increment_Seq_by_expression()
        {
            var db = CreatePocoDynamo();

            db.CreateTableIfMissing(db.RegisterTable<Seq>());

            var key = Guid.NewGuid().ToString();

            var nextId = db.IncrementById<Seq>(key, x => x.Counter);
            Assert.That(nextId, Is.EqualTo(1));

            nextId = db.IncrementById<Seq>(key, x => x.Counter);
            Assert.That(nextId, Is.EqualTo(2));

            nextId = db.DecrementById<Seq>(key, x => x.Counter);
            Assert.That(nextId, Is.EqualTo(1));
        }
    }
}