using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class PocoDynamoCreateTables : DynamoTestBase
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
        }

        [Test]
        public void Does_create_table_using_dynamodb_attributes()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithDynamoAttributes>();

            var table = DynamoMetadata.GetTable<TableWithDynamoAttributes>();

            Assert.That(table.HashKey.Name, Is.EqualTo("D"));
            Assert.That(table.RangeKey.Name, Is.EqualTo("C"));
            Assert.That(table.Fields.Length, Is.EqualTo(5));
        }

        [Test]
        public void Does_create_table_using_id_convention()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithIdConvention>();

            var table = DynamoMetadata.GetTable<TableWithIdConvention>();

            Assert.That(table.HashKey.Name, Is.EqualTo("Id"));
            Assert.That(table.RangeKey.Name, Is.EqualTo("RangeKey"));
            Assert.That(table.Fields.Length, Is.EqualTo(3));
        }

        [Test]
        public void Does_create_table_using_convention_names()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithConventionNames>();

            var table = DynamoMetadata.GetTable<TableWithConventionNames>();

            Assert.That(table.HashKey.Name, Is.EqualTo("HashKey"));
            Assert.That(table.RangeKey.Name, Is.EqualTo("RangeKey"));
            Assert.That(table.Fields.Length, Is.EqualTo(3));
        }

        [Test]
        public void Does_create_table_using_composite_index()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithCompositeKey>();

            var table = DynamoMetadata.GetTable<TableWithCompositeKey>();

            Assert.That(table.HashKey.Name, Is.EqualTo("D"));
            Assert.That(table.RangeKey.Name, Is.EqualTo("C"));
            Assert.That(table.Fields.Length, Is.EqualTo(5));
        }

        [Test]
        public void Does_create_table_and_index_using_Interface_attrs()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithTypedGlobalIndex>();

            var table = DynamoMetadata.GetTable<TableWithTypedGlobalIndex>();

            Assert.That(table.HashKey.Name, Is.EqualTo("D"));
            Assert.That(table.RangeKey.Name, Is.EqualTo("C"));
            Assert.That(table.Fields.Length, Is.EqualTo(5));

            Assert.That(table.GlobalIndexes.Count, Is.EqualTo(1));
            Assert.That(table.GlobalIndexes[0].HashKey.Name, Is.EqualTo("B"));
            Assert.That(table.GlobalIndexes[0].RangeKey.Name, Is.EqualTo("D"));
        }

        [Test]
        public void Does_create_table_with_ProvisionedThroughput()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<TableWithProvision>();

            var table = DynamoMetadata.GetTable<TableWithProvision>();
            Assert.That(table.HashKey.Name, Is.EqualTo("Id"));
            Assert.That(table.ReadCapacityUnits, Is.EqualTo(100));
            Assert.That(table.WriteCapacityUnits, Is.EqualTo(50));
        }

        [Test]
        public void Does_create_table_with_GlobalIndex_with_ProvisionedThroughput()
        {
            var db = (PocoDynamo)CreatePocoDynamo();
            db.RegisterTable<TableWithGlobalIndexProvision>();

            var table = DynamoMetadata.GetTable<TableWithGlobalIndexProvision>();
            Assert.That(table.HashKey.Name, Is.EqualTo("Id"));
            Assert.That(table.ReadCapacityUnits, Is.Null);
            Assert.That(table.WriteCapacityUnits, Is.Null);
            Assert.That(table.GlobalIndexes[0].ReadCapacityUnits, Is.EqualTo(100));
            Assert.That(table.GlobalIndexes[0].WriteCapacityUnits, Is.EqualTo(50));
        }

        [Test]
        public void Can_put_UserAuth()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<UserAuth>();
            db.InitSchema();

            db.PutItem(new UserAuth
            {
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                UserName = "mythz",
            });
        }

        [Test]
        public void Can_put_CustomUserAuth()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<CustomUserAuth>();
            db.InitSchema();

            db.PutItem(new CustomUserAuth
            {
                Custom = "CustomUserAuth",
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                UserName = "demis.bellot@gmail.com",
            });
        }

        [Test]
        public void Does_create_Collection_Table()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Collection>();
            db.InitSchema();

            var table = db.GetTableMetadata<Collection>();
            Assert.That(table.GetField("Id").DbType, Is.EqualTo(DynamoType.Number));
            Assert.That(table.GetField("Title").DbType, Is.EqualTo(DynamoType.String));
            Assert.That(table.GetField("ArrayInts").DbType, Is.EqualTo(DynamoType.List));
            Assert.That(table.GetField("SetStrings").DbType, Is.EqualTo(DynamoType.StringSet));
            Assert.That(table.GetField("ArrayStrings").DbType, Is.EqualTo(DynamoType.List));
            Assert.That(table.GetField("ListInts").DbType, Is.EqualTo(DynamoType.List));
            Assert.That(table.GetField("ListStrings").DbType, Is.EqualTo(DynamoType.List));
            Assert.That(table.GetField("SetInts").DbType, Is.EqualTo(DynamoType.NumberSet));
            Assert.That(table.GetField("DictionaryInts").DbType, Is.EqualTo(DynamoType.Map));
            Assert.That(table.GetField("DictionaryStrings").DbType, Is.EqualTo(DynamoType.Map));
            Assert.That(table.GetField("PocoLookup").DbType, Is.EqualTo(DynamoType.Map));
            Assert.That(table.GetField("PocoLookupMap").DbType, Is.EqualTo(DynamoType.Map));
        }

        [Test]
        public void Can_put_empty_Collection()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Collection>();
            db.InitSchema();

            db.PutItem(new Collection
            {
                ArrayInts = new int[0],
                SetStrings = new HashSet<string>(),
                ArrayStrings = new string[0],
                ListInts = new List<int>(),
                ListStrings = new List<string>(),
                SetInts = new HashSet<int>(),
                DictionaryInts = new Dictionary<int, int>(),
                DictionaryStrings = new Dictionary<string, string>(),
                PocoLookup = new Dictionary<string, List<Poco>>(),
                PocoLookupMap = new Dictionary<string, List<Dictionary<string, Poco>>>(),
            });
        }

        [Test]
        public void Can_Create_and_put_populated_AllTypes()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<AllTypes>();
            db.InitSchema();

            var dto = new AllTypes
            {
                Id = 1,
                NullableId = 2,
                Byte = 3,
                Short = 4,
                Int = 5,
                Long = 6,
                UShort = 7,
                UInt = 8,
                ULong = 9,
                Float = 1.1f,
                Double = 2.2,
                Decimal = 3.3M,
                String = "String",
                DateTime = new DateTime(2001, 01, 01),
                TimeSpan = new TimeSpan(1, 1, 1, 1, 1),
                DateTimeOffset = new DateTimeOffset(new DateTime(2001, 01, 01)),
                Guid = new Guid("DC8837C3-84FB-401B-AB59-CE799FF99142"),
                Char = 'A',
                NullableDateTime = new DateTime(2001, 01, 01),
                NullableTimeSpan = new TimeSpan(1, 1, 1, 1, 1),
                StringList = new[] { "A", "B", "C" }.ToList(),
                StringArray = new[] { "D", "E", "F" },
                StringMap = new Dictionary<string, string>
                {
                    {"A","1"},
                    {"B","2"},
                    {"C","3"},
                },
                IntStringMap = new Dictionary<int, string>
                {
                    { 1, "A" },
                    { 2, "B" },
                    { 3, "C" },
                },
                SubType = new SubType
                {
                    Id = 1,
                    Name = "Name"
                }
            };

            db.PutItem(dto);

            var row = db.GetItem<AllTypes>(1);

            Assert.That(dto, Is.EqualTo(row));
        }

        [Test]
        public void Can_Create_and_put_empty_AllTypes()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<AllTypes>();
            db.InitSchema();

            var dto = new AllTypes { Id = 1 };

            db.PutItem(dto);

            var row = db.GetItem<AllTypes>(1);

            Assert.That(dto, Is.EqualTo(row));
        }
    }

}