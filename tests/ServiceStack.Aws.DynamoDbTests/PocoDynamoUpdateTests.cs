using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.DynamoDbTests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class PocoDynamoUpdateTests : DynamoTestBase
    {
        private static Customer CreateCustomer()
        {
            var customer = new Customer
            {
                Name = "Foo",
                Age = 27,
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
            return customer;
        }


        [Test]
        public void Can_UpdateItemNonDefaults_Partial_Customer()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            db.DeleteTable<Customer>();
            db.InitSchema();

            var customer = CreateCustomer();

            db.PutItem(customer);

            var row = db.UpdateItemNonDefaults(new Customer { Id = customer.Id, Age = 42 });
            row.PrintDump();

            var updatedCustomer = db.GetItem<Customer>(customer.Id);

            Assert.That(updatedCustomer.Age, Is.EqualTo(42));
            Assert.That(updatedCustomer.Name, Is.EqualTo(customer.Name));
            Assert.That(updatedCustomer.PrimaryAddress, Is.EqualTo(customer.PrimaryAddress));
            Assert.That(updatedCustomer.Orders, Is.EquivalentTo(customer.Orders));
        }

        [Test]
        public void Can_partial_UpdateItem_with_Dictionary()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            db.DeleteTable<Customer>();
            db.InitSchema();

            var customer = CreateCustomer();

            db.PutItem(customer);

            db.UpdateItem<Customer>(new DynamoUpdateItem
            {
                Hash = customer.Id,
                Put = new Dictionary<string, object>
                {
                    { "Nationality", "Australian" },
                },
                Add = new Dictionary<string, object>
                {
                    { "Age", -1 }
                },
                Delete = new[] { "Name", "Orders" },
            });

            var updatedCustomer = db.GetItem<Customer>(customer.Id);

            Assert.That(updatedCustomer.Age, Is.EqualTo(customer.Age - 1));
            Assert.That(updatedCustomer.Name, Is.Null);
            Assert.That(updatedCustomer.Nationality, Is.EqualTo("Australian"));
            Assert.That(updatedCustomer.PrimaryAddress, Is.EqualTo(customer.PrimaryAddress));
            Assert.That(updatedCustomer.Orders, Is.Null);
        }

        [Test]
        public void TypedApi_does_populate_DynamoUpdateItem()
        {
            var db = CreatePocoDynamo()
                .RegisterTable<Customer>();

            db.DeleteTable<Customer>();
            db.InitSchema();

            var customer = CreateCustomer();

            db.PutItem(customer);

            db.UpdateItem<Customer>(customer.Id, 
                put: x => x.Nationality = "Australian",
                add: x => x.Age = -1,
                delete: x => new { x.Name, x.Orders });

            var updatedCustomer = db.GetItem<Customer>(customer.Id);

            Assert.That(updatedCustomer.Age, Is.EqualTo(customer.Age - 1));
            Assert.That(updatedCustomer.Name, Is.Null);
            Assert.That(updatedCustomer.Nationality, Is.EqualTo("Australian"));
            Assert.That(updatedCustomer.PrimaryAddress, Is.EqualTo(customer.PrimaryAddress));
            Assert.That(updatedCustomer.Orders, Is.Null);
        }
    }
}