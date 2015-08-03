using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;
using ServiceStack.Server.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.PocoDynamoTests
{
    [TestFixture]
    public class PocoDynaboDbTests : DynamoTestBase
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
        }

        [Test]
        public void Does_Create_tables()
        {
            var db = CreatePocoDynamo();

            var allTablesCreated = CreateTestTables(db);

            Assert.That(allTablesCreated, Is.True);

            var tableNames = db.GetTableNames();

            Assert.That(tableNames, Is.EquivalentTo(new[] {
                "Customer",
                "CustomerAddress",
                "Order",
                "Country",
                "Node",
            }));
        }

        [Test]
        public void Can_put_and_delete_Country_raw()
        {
            var db = CreatePocoDynamo();

            CreateTestTables(db);

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
            var db = CreatePocoDynamo();

            CreateTestTables(db);

            var country = new Country
            {
                Id = 2,
                CountryCode = "US",
                CountryName = "United States"
            };

            db.PutItem(country);

            var dbCountry = db.GetItemById<Country>(2);

            dbCountry.PrintDump();

            Assert.That(dbCountry, Is.EqualTo(country));
        }

        [Test]
        public void Can_put_and_delete_basic_Customer_raw()
        {
            var db = CreatePocoDynamo();

            CreateTestTables(db);

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
        public void Can_Put_Get_and_Delete_Customer_with_Orders()
        {
            var db = CreatePocoDynamo();

            CreateTestTables(db);

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

            db.PutItem(customer);

            var dbCustomer = db.GetItemById<Customer>(1);

            dbCustomer.PrintDump();

            Assert.That(dbCustomer.Equals(customer));

            db.DeleteItemById<Customer>(1);

            dbCustomer = db.GetItemById<Customer>(1);

            Assert.That(dbCustomer, Is.Null);
        }

        [Test]
        public void Can_Put_Get_and_Delete_Deeply_Nested_Nodes()
        {
            var db = CreatePocoDynamo();

            CreateTestTables(db);

            var nodes = new Node(1, "/root",
                new List<Node>
                {
                    new Node(2,"/root/2", new[] {
                        new Node(4, "/root/2/4", new [] {
                            new Node(5, "/root/2/4/5", new[] {
                                new Node(6, "/root/2/4/5/6"), 
                            }), 
                        }),
                    }),
                    new Node(3, "/root/3")
                });

            db.PutItem(nodes);

            var dbNodes = db.GetItemById<Node>(1);

            dbNodes.PrintDump();

            Assert.That(dbNodes, Is.EqualTo(nodes));
        }
    }
}