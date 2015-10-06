using System;
using System.Linq.Expressions;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;

namespace ServiceStack.Aws.DynamoDbTests
{
    [TestFixture]
    public class PocoDynamoExpressionTests : DynamoTestBase
    {
        public PocoDynamoExpression Parse<T>(Expression<Func<T, bool>> predicate)
        {
            return PocoDynamoExpression.FactoryFn(typeof(T), predicate);
        }

        private static IPocoDynamo InitTypes()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Poco>();
            db.RegisterTable<Collection>();
            db.InitSchema();
            return db;
        }

        [Test]
        public void Does_serialize_expression()
        {
            InitTypes();

            var q = Parse<Poco>(x => x.Id < 5);

            Assert.That(q.FilterExpression, Is.EqualTo("(Id < :p0)"));
            Assert.That(q.Params.Count, Is.EqualTo(1));
            Assert.That(q.Params[":p0"], Is.EqualTo(5));
        }

        [Test]
        public void Does_serialize_begins_with()
        {
            InitTypes();

            var q = Parse<Poco>(x => x.Title.StartsWith("Name 1"));

            Assert.That(q.FilterExpression, Is.EqualTo("begins_with(Title, :p0)"));
            Assert.That(q.Params.Count, Is.EqualTo(1));
            Assert.That(q.Params[":p0"], Is.EqualTo("Name 1"));
        }

        [Test]
        public void Does_serialize_contains_set()
        {
            InitTypes();

            var q = Parse<Collection>(x => x.SetStrings.Contains("A"));

            Assert.That(q.FilterExpression, Is.EqualTo("contains(SetStrings, :p0)"));
            Assert.That(q.Params.Count, Is.EqualTo(1));
            Assert.That(q.Params[":p0"], Is.EqualTo("A"));
        }

        [Test]
        public void Does_serialize_not_contains_set()
        {
            InitTypes();

            var q = Parse<Collection>(x => !x.SetStrings.Contains("A"));

            Assert.That(q.FilterExpression, Is.EqualTo("not contains(SetStrings, :p0)"));
            Assert.That(q.Params.Count, Is.EqualTo(1));
            Assert.That(q.Params[":p0"], Is.EqualTo("A"));
        }
    }
}