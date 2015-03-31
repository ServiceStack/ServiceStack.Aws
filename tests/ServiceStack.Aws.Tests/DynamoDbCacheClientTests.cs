using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using NUnit.Framework;
using ServiceStack.Caching;

namespace ServiceStack.Aws.Tests
{
    [TestFixture]
    public class DynamoDbCacheClientTests
    {
        private ICacheClient cacheClient;

        [TestFixtureSetUp]
        public void OnTestFixtureSetup()
        {
            var config = new AmazonDynamoDBConfig()
            {
                ServiceURL = "http://192.168.137.217:8000"
            };

            var dynamoDbClient = new AmazonDynamoDBClient("keyId", "key", config);
            this.cacheClient = new DynamoDbCacheClient(dynamoDbClient, createTableIfMissing: true);
        }

        [Test]
        public void Can_set_get_and_remove()
        {
            this.cacheClient.Set("Car", "Audi");
            var response = this.cacheClient.Get<string>("Car");
            Assert.That(response, Is.EqualTo("Audi"));

            this.cacheClient.Remove("Car");
            response = this.cacheClient.Get<string>("Car");
            Assert.That(response, Is.EqualTo(default(string)));
        }

        [Test]
        public void Does_expire_key_with_local_time()
        {
            this.cacheClient.Set("Car", "Audi", DateTime.Now.AddMilliseconds(10000));

            var response = this.cacheClient.Get<string>("Car");
            Assert.That(response, Is.EqualTo("Audi"));

            this.cacheClient.Set("Car", "Audi", DateTime.Now.AddMilliseconds(-10000));

            response = this.cacheClient.Get<string>("Car");
            Assert.That(response, Is.EqualTo(default(string)));
        }

        [Test]
        public void Does_expire_key_with_utc_time()
        {
            this.cacheClient.Set("Car", "Audi", DateTime.UtcNow.AddMilliseconds(10000));

            var response = this.cacheClient.Get<string>("Car");
            Assert.That(response, Is.EqualTo("Audi"));

            this.cacheClient.Set("Car", "Audi", DateTime.UtcNow.AddMilliseconds(-10000));

            response = this.cacheClient.Get<string>("Car");
            Assert.That(response, Is.EqualTo(default(string)));
        }

        [Test]
        public void Can_use_batch_operations()
        {
            this.cacheClient.SetAll(new Dictionary<string, string>
            {
                { "Car", "Audi" },
                { "Phone", "MotoX" }
            });

            var response = this.cacheClient.GetAll<string>(new List<string> { "Car", "Phone" });
            Assert.That(response["Car"], Is.EqualTo("Audi"));
            Assert.That(response["Phone"], Is.EqualTo("MotoX"));

            var singleResponse = this.cacheClient.Get<string>("Phone");
            Assert.That(singleResponse, Is.EqualTo("MotoX"));

            this.cacheClient.RemoveAll(new List<string> { "Car", "Phone" } );

            response = this.cacheClient.GetAll<string>(new List<string> { "Car", "Phone" });
            Assert.That(response["Car"], Is.EqualTo(default(string)));
            Assert.That(response["Phone"], Is.EqualTo(default(string)));
        }

        [Test]
        public void Can_increment_and_decrement_values()
        {
            Assert.That(this.cacheClient.Increment("incr:a", 2), Is.EqualTo(2));
            Assert.That(this.cacheClient.Increment("incr:a", 3), Is.EqualTo(5));
            this.cacheClient.Remove("incr:a");

            Assert.That(this.cacheClient.Decrement("decr:a", 2), Is.EqualTo(-2));
            Assert.That(this.cacheClient.Decrement("decr:a", 3), Is.EqualTo(-5));
            this.cacheClient.Remove("decr:a");
        }

        [Test]
        public void Can_cache_multiple_items_in_parallel()
        {
            var fns = 10.Times(i => (Action)(() =>
            {
                this.cacheClient.Set("concurrent-test", "Data: {0}".Fmt(i));
            }));

            Parallel.Invoke(fns.ToArray());

            var entry = this.cacheClient.Get<string>("concurrent-test");
            Assert.That(entry, Is.StringStarting("Data: "));

            this.cacheClient.Remove("concurrent-test");
        }

        [Test]
        public void Does_flush_all()
        {
            3.Times(i => this.cacheClient.Set("Car" + i, "Audi"));
            3.Times(i => Assert.That(this.cacheClient.Get<string>("Car" + i), Is.EqualTo("Audi")));

            this.cacheClient.FlushAll();

            3.Times(i => Assert.That(this.cacheClient.Get<string>("Car" + i), Is.EqualTo(default(string))));
            3.Times(i => this.cacheClient.Remove("Car" + i));
        }

        [Test]
        public void Can_flush_and_set_in_parallel()
        {
            //Ensure that no exception is thrown even while the cache is being flushed
            Parallel.Invoke(
                () => this.cacheClient.FlushAll(),
                () =>
                {
                    5.Times(() =>
                    {
                        this.cacheClient.Set("Car1", "Ford");
                        Thread.Sleep(75);
                    });
                },
                () =>
                {
                    5.Times(() =>
                    {
                        this.cacheClient.Set("Car2", "Audi");
                        Thread.Sleep(50);
                    });
                });

            this.cacheClient.RemoveAll(new List<string> { "Car1", "Car2", "Car3" });
        }

        class Car
        {
            public string Manufacturer { get; set; }
            public int Age { get; set; }
        }

        [Test]
        public void Can_cache_complex_entry()
        {
            var car = new Car {Manufacturer = "Audi", Age = 3};
            this.cacheClient.Set("Car", car);

            var response = this.cacheClient.Get<Car>("Car");
            Assert.That(response.Manufacturer, Is.EqualTo(car.Manufacturer));
            Assert.That(response.Age, Is.EqualTo(car.Age));

            this.cacheClient.Remove("Car");
        }

        [Test]
        public void Does_only_add_if_key_does_not_exist()
        {
            Assert.IsTrue(this.cacheClient.Add("Car", "Audi"));
            Assert.That(this.cacheClient.Get<string>("Car"), Is.EqualTo("Audi"));

            Assert.IsFalse(this.cacheClient.Add("Car", "Ford"));
            Assert.That(this.cacheClient.Get<string>("Car"), Is.EqualTo("Audi"));
        }

        [Test]
        public void Does_only_replace_if_key_exists()
        {
            Assert.IsFalse(this.cacheClient.Replace("Car", "Audi"));
            Assert.That(this.cacheClient.Get<string>("Car"), Is.EqualTo(default(string)));

            this.cacheClient.Add("Car", "Ford");

            Assert.IsTrue(this.cacheClient.Replace("Car", "Audi"));
            Assert.That(this.cacheClient.Get<string>("Car"), Is.EqualTo("Audi"));
        }
    }
}
