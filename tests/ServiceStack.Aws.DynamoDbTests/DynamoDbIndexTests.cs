using System;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    //Poco Data Model for OrmLite + SeedData 
    [Route("/rockstars", "POST")]
    [References(typeof(RockstarAgeIndex))]
    public class Rockstar
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public bool Alive { get; set; }

        public string Url
        {
            get { return "/stars/{0}/{1}/".Fmt(Alive ? "alive" : "dead", LastName.ToLower()); }
        }

        public Rockstar() { }
        public Rockstar(int id, string firstName, string lastName, int age, bool alive)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Age = age;
            Alive = alive;
        }
    }

    public class RockstarAgeIndex : IGlobalIndex<Rockstar>
    {
        [HashKey]
        public int Age { get; set; }

        [RangeKey]
        public int Id { get; set; }
    }

    [TestFixture]
    public class DynamoDbIndexTests : DynamoTestBase
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
        }

        [Test]
        public void Does_not_create_or_project_readonly_fields()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Rockstar>();
            db.InitSchema();

            var expectedFields = "Id, FirstName, LastName, Age, Alive";
            var table = db.GetTableMetadata<Rockstar>();
            Assert.That(string.Join(", ", table.Fields.Map(x => x.Name)), Is.EqualTo(expectedFields));

            var q = db.FromQueryIndex<RockstarAgeIndex>(x => x.Age == 27);
            q.SelectInto<Rockstar>();

            q.ProjectionExpression.Print();
            Assert.That(q.ProjectionExpression, Is.EqualTo(expectedFields));
        }
    }
}