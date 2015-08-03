using System;
using Amazon.DynamoDBv2.DataModel;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.PocoDynamoTests
{
    public class RangeTest
    {
        public string Id { get; set; }

        [DynamoDBRangeKey]
        public DateTime CreatedDate { get; set; }

        public string Data { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }


    public class RangeKeyTests : DynamoTestBase
    {
        [Test]
        public void Can_Create_RangeTest()
        {
            var db = CreatePocoDynamo();

            db.CreateTableIfMissing(db.RegisterTable<RangeTest>());

            var createdDate = DateTime.UtcNow;
            db.PutItem(new RangeTest {
                Id = "test",
                CreatedDate = createdDate,
                Data = "Data",
            });

            var dto = db.GetItemByHashAndRange<RangeTest>("test", createdDate);

            dto.PrintDump();

            Assert.That(dto.Id, Is.EqualTo("test"));
            Assert.That(dto.Data, Is.EqualTo("Data"));
            Assert.That(dto.CreatedDate, Is.EqualTo(createdDate));
        }
    }
}