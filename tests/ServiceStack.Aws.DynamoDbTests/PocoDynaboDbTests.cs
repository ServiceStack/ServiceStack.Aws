using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.Model;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
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
            var types = new List<Type>()
                .Add<Customer>()
                .Add<Country>()
                .Add<Node>();

            db.RegisterTables(types);
            db.InitSchema();

            var tableNames = db.GetTableNames();

            var expected = new[] {
                "Customer",
                "Country",
                "Node",
            };
            Assert.That(expected.All(x => tableNames.Contains(x)));
        }

        [Test]
        public void Can_put_and_delete_Country_raw()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Country>();
            db.InitSchema();

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
            db.RegisterTable<Country>();
            db.InitSchema();

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
            db.RegisterTable<Customer>();
            db.InitSchema();

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
            db.RegisterTable<Customer>();
            db.InitSchema();

            var customer = new Customer
            {
                Id = 11,
                Name = "Foo",
                Orders = new List<Order>
                {
                    new Order
                    {
                        Id = 21,
                        CustomerId = 11,
                        LineItem = "Item 1",
                        Qty = 3,
                        Cost = 2,
                    },
                    new Order
                    {
                        Id = 22,
                        CustomerId = 11,
                        LineItem = "Item 2",
                        Qty = 4,
                        Cost = 3,
                    },
                },
                PrimaryAddress = new CustomerAddress
                {
                    Id = 31,
                    CustomerId = 11,
                    AddressLine1 = "Line 1",
                    AddressLine2 = "Line 2",
                    City = "Darwin",
                    State = "NT",
                    Country = "AU",
                }
            };

            db.PutItem(customer);

            var dbCustomer = db.GetItemById<Customer>(11);

            Assert.That(dbCustomer.Equals(customer));

            db.DeleteItemById<Customer>(11);

            dbCustomer = db.GetItemById<Customer>(11);

            Assert.That(dbCustomer, Is.Null);
        }

        [Test]
        public void Does_auto_populate_AutoIncrement_fields()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Customer>();
            db.InitSchema();

            db.Sequences.Reset<Customer>(10);
            db.Sequences.Reset<Order>(20);
            db.Sequences.Reset<CustomerAddress>(30);

            var customer = new Customer
            {
                Name = "Foo",
            };

            db.PutItem(customer);

            Assert.That(customer.Id, Is.EqualTo(11));

            Assert.That(db.Sequences.Current<Customer>(), Is.EqualTo(11));
            Assert.That(db.Sequences.Current<Order>(), Is.EqualTo(20));
            Assert.That(db.Sequences.Current<CustomerAddress>(), Is.EqualTo(30));

            var dbCustomer = db.GetItemById<Customer>(11);
            Assert.That(dbCustomer.Id, Is.EqualTo(11));

            customer = new Customer
            {
                Name = "Foo",
                Orders = new List<Order>
                {
                    new Order
                    {
                        LineItem = "Item 1",
                        Qty = 3,
                        Cost = 2,
                    },
                    new Order
                    {
                        LineItem = "Item 2",
                        Qty = 4,
                        Cost = 3,
                    },
                },
                PrimaryAddress = new CustomerAddress
                {
                    AddressLine1 = "Line 1",
                    AddressLine2 = "Line 2",
                    City = "Darwin",
                    State = "NT",
                    Country = "AU",
                }
            };

            db.PutItem(customer);

            Assert.That(customer.Id, Is.EqualTo(12));
            Assert.That(customer.Orders[0].Id, Is.EqualTo(21));
            Assert.That(customer.Orders[1].Id, Is.EqualTo(22));
            Assert.That(customer.PrimaryAddress.Id, Is.EqualTo(31));
        }

        [Test]
        public void Can_Put_Get_and_Delete_Deeply_Nested_Nodes()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Node>();
            db.InitSchema();

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

        [Test]
        public void Can_GetAll()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Poco>();
            db.InitSchema();

            var items = 10.Times(i => new Poco { Id = i, Name = "Name " + i });

            items.Each(x => db.PutItem(x));

            var results = db.GetAll<Poco>();

            Assert.That(results, Is.EquivalentTo(items));
        }

        [Test]
        public void Can_Batch_PutItems_and_GetItems()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Poco>();
            db.InitSchema();

            var items = 10.Times(i => new Poco { Id = i, Name = "Name " + i });

            db.PutItems(items);

            var results = db.GetItemsByIds<Poco>(items.Map(x => x.Id));

            Assert.That(results, Is.EquivalentTo(items));
        }

        [Test]
        public void Batch_PutItems_and_GetItems_handles_multiple_batches()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Poco>();
            db.InitSchema();

            var items = 110.Times(i => new Poco { Id = i, Name = "Name " + i });

            db.PutItems(items);

            var results = db.GetItemsByIds<Poco>(items.Map(x => x.Id));

            Assert.That(results, Is.EquivalentTo(items));
        }

        [Test]
        public void Can_Scan_by_FilterExpression()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Poco>();
            db.InitSchema();

            var items = 10.Times(i => new Poco { Id = i, Name = "Name " + i });

            db.PutItems(items);

            var low5 = db.Scan<Poco>("Id < :Count",
                new Dictionary<string, object> { { "Count", 5 } })
                .ToList();

            low5.PrintDump();

            var expected = items.Where(x => x.Id < 5).ToList();
            Assert.That(low5.Count, Is.EqualTo(5));
            Assert.That(low5, Is.EquivalentTo(expected));

            low5 = db.Scan<Poco>("Id < :Count", new { Count = 5 }).ToList();
            Assert.That(low5, Is.EquivalentTo(expected));

            low5 = db.Scan<Poco>(x => x.Id < 5).ToList();
            Assert.That(low5, Is.EquivalentTo(expected));
        }

        [Test]
        public void Can_Scan_by_FilterExpression_with_Limit()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<Poco>();
            db.InitSchema();

            var items = 10.Times(i => new Poco { Id = i, Name = "Name " + i });

            db.PutItems(items);

            var low5 = db.Scan<Poco>("Id < :Count",
                new Dictionary<string, object> { { "Count", 5 } }, limit:5);

            low5.PrintDump();

            var expected = items.Where(x => x.Id < 5).ToList();
            Assert.That(low5.Count, Is.EqualTo(5));
            Assert.That(low5, Is.EquivalentTo(expected));

            low5 = db.Scan<Poco>("Id < :Count", new { Count = 5 }, limit: 5);
            Assert.That(low5, Is.EquivalentTo(expected));

            low5 = db.Scan<Poco>(x => x.Id < 5, limit: 5);
            Assert.That(low5, Is.EquivalentTo(expected));

            var low3 = db.Scan<Poco>("Id < :Count", new { Count = 5 }, limit: 3);
            Assert.That(low3.Count, Is.EqualTo(3));

            low3 = db.Scan<Poco>(x => x.Id < 5, limit: 3);
            Assert.That(low3.Count, Is.EqualTo(3));
        }
    }
}