using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class PocoDynamoRelatedTables : DynamoTestBase
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));
        }

        [Test]
        public void Does_generate_correct_metadata_for_Related_Order_Table()
        {
            var db = CreatePocoDynamo();

            var types = new List<Type>()
                .Add<Customer>()
                .Add<Order>();

            db.RegisterTables(types);

            var tables = DynamoMetadata.GetTables();
            var customerTable = tables.First(x => x.Type == typeof(Customer));
            var orderTable = tables.First(x => x.Type == typeof(Order));

            Assert.That(customerTable.HashKey.Name, Is.EqualTo("Id"));
            Assert.That(customerTable.RangeKey, Is.Null);

            Assert.That(orderTable.HashKey.Name, Is.EqualTo("CustomerId"));
            Assert.That(orderTable.RangeKey.Name, Is.EqualTo("Id"));
        }

        [Test]
        public void Can_Get_from_Related_Order_Table_from_Customer()
        {
            var db = CreatePocoDynamo();
            var types = new List<Type>()
                .Add<Customer>()
                .Add<Order>();

            db.RegisterTables(types);
            db.InitSchema();

            var customer = new Customer
            {
                Name = "Customer #1",
                PrimaryAddress = new CustomerAddress
                {
                    AddressLine1 = "Line 1",
                    City = "Darwin",
                    State = "NT",
                    Country = "AU",
                }
            };

            db.PutItem(customer);

            Assert.That(customer.Id, Is.GreaterThan(0));
            Assert.That(customer.PrimaryAddress.Id, Is.GreaterThan(0));

            var orders = new []
            {
                new Order
                {
                    CustomerId = 11,
                    LineItem = "Item 1",
                    Qty = 3,
                    Cost = 2,
                },
                new Order
                {
                    CustomerId = 11,
                    LineItem = "Item 2",
                    Qty = 4,
                    Cost = 3,
                },
            };

            db.PutRelated(customer, orders);

            Assert.That(orders[0].Id, Is.GreaterThan(0));
            Assert.That(orders[0].CustomerId, Is.EqualTo(customer.Id));
            Assert.That(orders[1].Id, Is.GreaterThan(0));
            Assert.That(orders[1].CustomerId, Is.EqualTo(customer.Id));

            var dbOrders = db.GetRelated<Order>(customer).ToList();

            Assert.That(dbOrders, Is.EquivalentTo(orders));
        }
    }
}