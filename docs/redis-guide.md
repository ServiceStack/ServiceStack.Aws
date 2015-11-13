# Getting started with AWS ElastiCache
## ServiceStack.Redis

![](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticache-redis-powered-by-aws.png)

Amazon's 'ElastiCache' allows a simple way to create and manage cache instances that can be simply incorporated into your ServiceStack application stack using the ServiceStack Redis client, `ServiceStack.Redis`. 

#### Creating an ElastiCache Cluster

1. Login to the [AWS Web console](https://console.aws.amazon.com/console/home).
2. Select [ElastiCache](https://console.aws.amazon.com/elasticache/home) from the **Services** from the top menu.
![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/aws-services-menu-elasticcache.png)
3. Select **Get Started Now** or **ElasticCache Dashboard** and **Launch Cache Cluster**
4. Select **Redis** for the cluster engine.

You can run your cache as a single Redis node or add multiple nodes for additional redundency. In this example, we will be using 3 nodes. One as a master node and 2 read only replicas. 

![](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticcache-redis-config.png)
> To use the smaller instances like the `cache.t2.micro`, **Multi-AZ** must be disabled.


So you're EC2 instance can access your Redis nodes, ensure you select a **VPC Security Group** that exposes the default port `6379`.

![](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticcache-redis-adv.png)
> If you haven't already setup a security group exposing this port, you'll need to create one by [managing your VPC security groups](https://console.aws.amazon.com/vpc/home#securityGroups:).

To finish, reviewed your settings and click **Launch Replication Group**.

## Enable Caching with ServiceStack.Redis
Now you're your Redis nodes are ready, your AppHost can be configured to use them when deployed. AWS **does not allow external access** to ElastiCache servers, so they can only be used when your ServiceStack application is deployed.

First, you'll need to install `ServiceStack.Redis` NuGet package if your application doesn't already use it.

![](https://github.com/ServiceStack/Assets/raw/master/img/aws/nuget-install-redis.png)

In this example, we are going to use a `PooledRedisClientManager` for our `IRedisClientsManager`. This will be responsible for creating `ICacheClient`s that our `Service`s will use to connect to the ElastiCache nodes. We will need to provide our `PooledRedisClientManager` with the nodes we have create. For example, as shown above, we created a cluster of **1 Primary** and **2 Read Replicas**, these endpoint URLs can be accessed from the ElastiCache **Dashboard**.

![](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticcache-redis-nodes.png)

Below is a simple example of a configured self hosting AppHost that uses ElastiCache for caching when deployed and an in memory caching when developing locally.

``` csharp
public class AppHost : AppSelfHostBase
{
    public AppHost() : base("AWS ElastiCache Example", typeof(CustomerService).Assembly) {}

    public override void Configure(Container container)
    {
        if (AppSettings.GetString("Environment") == "Production")
        {
            container.Register<IRedisClientsManager>(c =>
                new PooledRedisClientManager(
                    // Primary node
                    new[]
                    {
                        "redis-cluster-001.jbnmsd.0001.apse2.cache.amazonaws.com"
                    },
                    // Read replicas
                    new[]
                    {
                        "redis-cluster-002.jbnmsd.0001.apse2.cache.amazonaws.com",
                        "redis-cluster-003.jbnmsd.0001.apse2.cache.amazonaws.com"
                    }));

            container.Register<ICacheClient>(c =>
                container.Resolve<IRedisClientsManager>().GetCacheClient());
        }
        else
        {
            container.Register<ICacheClient>(new MemoryCacheClient());
        }
    }
}

```

Now that your caching is setup and connecting, you can cache your web servie responses easily by returning `Request.ToOptimizedResultUsingCache` from within a ServiceStack `Service`. For example, returning a full customers details might be an expensive database query. We can cache the result in the ElastiCache cluster for a faster response and invalidate the cache when the details are updated.

``` csharp
public class CustomerService : Service
{
    private static string CacheKey = "customer_details_{0}";

    public object Get(GetCustomerDetails request)
    {
        return this.Request.ToOptimizedResultUsingCache(this.Cache,
            CacheKey.Fmt(request.CustomerId), 
                () => new GetCustomerDetailsResponse {
                    Result = this.Db.LoadSingleById<CustomerDetails>(request.CustomerId)
        });
    }

    public object Put(UpdateCustomerDetails request)
    {
        var customer = this.Db.LoadSingleById<CustomerDetails>(request.CustomerId);
        customer = request.ConvertTo<CustomerDetails>().PopulateWith(customer);
        this.Db.Update(customer);
        //Invalidate customer details cache
        this.Cache.ClearCaches(CacheKey.Fmt(request.CustomerId));
        return new UpdateCustomerDetailsResponse()
        {
            Result = customer
        };
    }
}
```