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
        [Test]
        public void Does_serialize_expression()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Poco>();
            db.InitSchema();

            var q = Parse<Poco>(x => x.Id < 5);

            Assert.That(q.FilterExpression, Is.EqualTo("(Id < :p0)"));
            Assert.That(q.Params.Count, Is.EqualTo(1));
            Assert.That(q.Params[":p0"], Is.EqualTo(5));
        }

        public PocoDynamoExpression Parse<T>(Expression<Func<T, bool>> predicate)
        {
            return PocoDynamoExpression.FactoryFn(typeof (T), predicate);
        }
    }
}