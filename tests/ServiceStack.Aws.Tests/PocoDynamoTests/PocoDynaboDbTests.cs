using System;
using System.Collections.Generic;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Server.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.PocoDynamoTests
{
    [TestFixture]
    public class PocoDynaboDbTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var db = CreateClient();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
        }

        public static bool CreateCustomerTables(PocoDynamo db)
        {
            var types = new List<Type>()
                .Add<Customer>()
                .Add<CustomerAddress>()
                .Add<Order>()
                .Add<Country>();

            var tables = DynamoMetadata.RegisterTables(types);
            var allTablesCreated = db.CreateNonExistingTables(tables, TimeSpan.FromMinutes(1));
            return allTablesCreated;
        }

        [Test]
        public void Does_Create_tables()
        {
            var db = CreateClient();

            var allTablesCreated = CreateCustomerTables(db);

            Assert.That(allTablesCreated, Is.True);

            var tableNames = db.GetTableNames();

            Assert.That(tableNames, Is.EquivalentTo(new[] {
                "Customer",
                "CustomerAddress",
                "Order",
                "Country",
            }));
        }

        [Test]
        public void Can_put_and_delete_Country_raw()
        {
            var db = CreateClient();

            CreateCustomerTables(db);

            db.DynamoDb.PutItem(new PutItemRequest
            {
                TableName = typeof(Country).Name,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { N = "1" } },
                    { "CountryName", new AttributeValue { S = "Australia"} },
                    { "CountryCode", new AttributeValue { S = "AU"} },
                }
            });

            var response = db.DynamoDb.GetItem(new GetItemRequest
            {
                TableName = typeof(Country).Name,
                ConsistentRead = true,
                Key = new Dictionary<string, AttributeValue> {
                    { "Id", new AttributeValue { N = "1" } }
                }
            });

            Assert.That(response.IsItemSet);
            Assert.That(response.Item["Id"].N, Is.EqualTo("1"));
            Assert.That(response.Item["CountryName"].S, Is.EqualTo("Australia"));
            Assert.That(response.Item["CountryCode"].S, Is.EqualTo("AU"));
        }

        [Test]
        public void Can_put_and_delete_Country()
        {
            var db = CreateClient();

            CreateCustomerTables(db);

            var country = new Country
            {
                Id = 2,
                CountryCode = "US",
                CountryName = "United States"
            };

            db.Put(country);

            var dbCountry = db.GetById<Country>(2);

            dbCountry.PrintDump();

            Assert.That(dbCountry, Is.EqualTo(country));
        }

        [Test]
        public void Can_put_and_delete_basic_Customer_raw()
        {
            var db = CreateClient();

            CreateCustomerTables(db);

            db.DynamoDb.PutItem(new PutItemRequest
            {
                TableName = typeof(Customer).Name,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "Id", new AttributeValue { N = "2" } },
                    { "Name", new AttributeValue { S = "Foo"} },
                    { "Orders", new AttributeValue { NULL = true } },
                    { "CustomerAddress", new AttributeValue { NULL = true } },
                }
            });

            var response = db.DynamoDb.GetItem(new GetItemRequest
            {
                TableName = typeof(Customer).Name,
                ConsistentRead = true,
                Key = new Dictionary<string, AttributeValue> {
                    { "Id", new AttributeValue { N = "2" } }
                }
            });

            Assert.That(response.IsItemSet);
            Assert.That(response.Item["Id"].N, Is.EqualTo("2"));
            Assert.That(response.Item["Name"].S, Is.EqualTo("Foo"));
            Assert.That(response.Item["Orders"].NULL);
            Assert.That(response.Item["CustomerAddress"].NULL);
        }

        [Test]
        public void Can_Put_and_Delete_Customer_with_Orders()
        {
            var db = CreateClient();

            CreateCustomerTables(db);

            var customer = new Customer {
                Id = 1,
                Name = "Foo",
                Orders = new List<Order>
                {
                    new Order
                    {
                        Id = 1,
                        CustomerId = 1,
                        LineItem = "Item 1",
                        Qty = 3,
                        Cost = 2,
                    },
                    new Order
                    {
                        Id = 2,
                        CustomerId = 1,
                        LineItem = "Item 2",
                        Qty = 4,
                        Cost = 3,
                    },
                },
                PrimaryAddress = new CustomerAddress
                {
                    Id = 1,
                    CustomerId = 1,
                    AddressLine1 = "Line 1",
                    AddressLine2 = "Line 2",
                    City = "Darwin",
                    State = "NT",
                    Country = "AU",
                }
            };

            db.Put(customer);

            var dbCustomer = db.GetById<Customer>(1);

            dbCustomer.PrintDump();

            Assert.That(dbCustomer.Equals(customer));
        }

        public static PocoDynamo CreateClient()
        {
            var accessKey = Environment.GetEnvironmentVariable("AWSAccessKey");
            var secretKey = Environment.GetEnvironmentVariable("AWSSecretKey");

            var useLocalDb = string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey);

            var dynamoClient = useLocalDb
                ? new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig {
                        ServiceURL = ConfigUtils.GetAppSetting("DynamoDbUrl", "http://localhost:8000"),
                    })
                : new AmazonDynamoDBClient(accessKey, secretKey, RegionEndpoint.USEast1);

            var db = new PocoDynamo(dynamoClient);
            return db;
        }
    }
}