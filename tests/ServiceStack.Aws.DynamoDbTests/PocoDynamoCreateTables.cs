using System;
using System.Collections.Generic;
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
            db.RegisterTable<TableWithCompositeIndex>();

            var table = DynamoMetadata.GetTable<TableWithCompositeIndex>();

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
            });
        }
    }

}