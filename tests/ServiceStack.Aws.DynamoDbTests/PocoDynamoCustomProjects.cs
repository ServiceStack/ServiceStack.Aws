using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class PocoDynamoCustomProjects : DynamoTestBase
    {
        [Test]
        public void Can_select_single_Name_field()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
            db.RegisterTable<Customer>();
            db.InitSchema();
                
            db.PutItems(new[] {
                new Customer { Name = "John", Age = 27, Nationality = "Australian" }, 
                new Customer { Name = "Jill", Age = 27, Nationality = "USA" }, 
            });
                
            var q = db.FromScan<Customer>();
            var results = q.Select<Customer>(x => new { x.Name }).Exec();
                
            Assert.That(results.All(x => x.Nationality == null));
            Assert.That(results.All(x => x.Age == null));
            Assert.That(results.Map(x => x.Name), Is.EquivalentTo(new[]{ "John", "Jill" }));
        }
    }
}