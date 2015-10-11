using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class PocoDynamoRelatedTables : DynamoTestBase
    {
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
            var customer = CreateCustomer(db);

            var orders = new[]
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

            db.PutRelated(customer.Id, orders);

            Assert.That(customer.Id, Is.GreaterThan(0));
            Assert.That(customer.PrimaryAddress.Id, Is.GreaterThan(0));

            Assert.That(orders[0].Id, Is.GreaterThan(0));
            Assert.That(orders[0].CustomerId, Is.EqualTo(customer.Id));
            Assert.That(orders[1].Id, Is.GreaterThan(0));
            Assert.That(orders[1].CustomerId, Is.EqualTo(customer.Id));

            var dbOrders = db.GetRelated<Order>(customer.Id).ToList();

            Assert.That(dbOrders, Is.EquivalentTo(orders));
        }

        [Test]
        public void Can_Create_LocalIndex()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<OrderWithFieldIndex>();

            var customer = CreateCustomer(db);

            var orders = 10.Times(i => new OrderWithFieldIndex
            {
                LineItem = "Item " + (i + 1),
                Qty = i + 2,
                Cost = (i + 2) * 2
            });

            db.PutRelated(customer.Id, orders);

            var expensiveOrders = db.QueryRelated<OrderWithFieldIndex>(
                x => x.CustomerId == customer.Id && x.Cost > 10).ToList();

            Assert.That(expensiveOrders.Count, Is.EqualTo(orders.Count(x => x.Cost > 10)));
            Assert.That(expensiveOrders.All(x => x.Qty == 0));  //non-projected field

            expensiveOrders = db.QueryRelated<OrderWithFieldIndex>(x => 
                x.CustomerId == customer.Id && x.Cost > 10, new [] { "Qty" }).ToList();
            Assert.That(expensiveOrders.All(x => x.Id == 0));
            Assert.That(expensiveOrders.All(x => x.Qty > 0));

            expensiveOrders = db.QueryRelated<OrderWithFieldIndex>(x => 
                x.CustomerId == customer.Id && x.Cost > 10, typeof(OrderWithFieldIndex).AllFields()).ToList();
            Assert.That(expensiveOrders.All(x => x.Cost > 10 && x.Id > 0 && x.CustomerId > 0 && x.Qty > 0 && x.LineItem != null));

            expensiveOrders = db.QueryRelated<OrderWithFieldIndex>(x => 
                x.CustomerId == customer.Id && x.Cost > 10, x => new { x.Id, x.Cost }).ToList();
            Assert.That(expensiveOrders.All(x => x.CustomerId == 0));
            Assert.That(expensiveOrders.All(x => x.Cost > 10 && x.Id > 0));

            Assert.Throws<ArgumentException>(() =>
                db.QueryRelated<OrderWithFieldIndex>(x => x.Cost > 10).ToList());
        }

        [Test]
        public void Can_Create_Typed_LocalIndex()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<OrderWithLocalTypedIndex>();

            var customer = CreateCustomer(db);

            var table = DynamoMetadata.GetTable<OrderWithLocalTypedIndex>();
            Assert.That(table.LocalIndexes.Count, Is.EqualTo(1));
            Assert.That(table.LocalIndexes[0].Name, Is.EqualTo("CustomCostIndex")); //Alias

            var orders = 10.Times(i => new OrderWithLocalTypedIndex
            {
                LineItem = "Item " + (i + 1),
                Qty = i + 2,
                Cost = (i + 2) * 2
            });

            db.PutRelated(customer.Id, orders);

            var expensiveOrders = db.QueryIndex<OrderCostIndex>(x => x.CustomerId == customer.Id && x.Cost > 10).ToList();

            expensiveOrders.PrintDump();

            Assert.That(expensiveOrders.Count, Is.EqualTo(orders.Count(x => x.CustomerId == customer.Id && x.Cost > 10)));
            Assert.That(expensiveOrders.All(x => x.CustomerId == customer.Id));
            Assert.That(expensiveOrders.All(x => x.Cost > 10 && x.Id > 0 && x.Qty > 0));

            Assert.Throws<AmazonDynamoDBException>(() =>
                db.QueryIndex<OrderCostIndex>(x => x.Cost > 10).ToList());
        }

        [Test]
        public void Can_Create_Typed_GlobalIndex()
        {
            var db = CreatePocoDynamo();
            db.RegisterTable<OrderWithGlobalTypedIndex>();

            var customer = CreateCustomer(db);

            var table = DynamoMetadata.GetTable<OrderWithGlobalTypedIndex>();
            Assert.That(table.GlobalIndexes.Count, Is.EqualTo(1));
            Assert.That(table.GlobalIndexes[0].Name, Is.EqualTo(typeof(OrderGlobalCostIndex).Name));
            Assert.That(table.GlobalIndexes[0].HashKey.Name, Is.EqualTo("ProductId"));
            Assert.That(table.GlobalIndexes[0].RangeKey.Name, Is.EqualTo("Cost"));

            var orders = 10.Times(i => new OrderWithGlobalTypedIndex
            {
                ProductId = 1,
                Qty = i + 2,
                Cost = (i + 2) * 2
            });

            db.PutRelated(customer.Id, orders);

            var expensiveOrders = db.QueryIndex<OrderGlobalCostIndex>(x => x.ProductId == 1 && x.Cost > 10).ToList();

            expensiveOrders.PrintDump();

            Assert.That(expensiveOrders.Count, Is.EqualTo(orders.Count(x => x.ProductId == 1 && x.Cost > 10)));
            Assert.That(expensiveOrders.All(x => x.Cost > 10 && x.Id > 0 && x.Qty > 0));
        }

        private static Customer CreateCustomer(IPocoDynamo db)
        {
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
            return customer;
        }
    }
}