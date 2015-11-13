## Getting started with AWS ElastiCache and ServiceStack

![](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticache-memcached-powered-by-aws.png)

### ServiceStack.Caching.Memcached

Amazon's 'ElastiCache' allows a simple way to create and manage Memcached instances that can be simply incorporated into your ServiceStack application stack using the ServiceStack NuGet package, `ServiceStack.Caching.Memcached`. 

#### Creating an ElastiCache Cluster

1. Login to the [AWS Web console](https://console.aws.amazon.com/console/home).
2. Select [ElastiCache](https://console.aws.amazon.com/elasticache/home) from the **Services** from the top menu.
![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/aws-services-menu-elasticcache.png)
3. Select **Get Started Now** or **ElasticCache Dashboard** and **Launch Cache Cluster**
4. Select **Memcached** for the cluster engine.

ElastiCache setup allows you to specify how many nodes you want in your cache cluster. In this example, we will be using 3.

![](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticcache-memcached-config.png)

So you're EC2 instance can access your Memcached cluster, ensure you select a **VPC Security Group** that exposes the default port `11211`. 

![](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticcache-memcached-adv.png)
> If you haven't already setup a security group exposing this port, you'll need to create one by [managing your VPC security groups](https://console.aws.amazon.com/vpc/home#securityGroups:).

To finish, reviewed your settings and click **Launch Cache Cluster**.

## Enable Caching in your ServiceStack application
Now you're your Memcached cluster is ready, your AppHost can be configured to use it when deployed. AWS **does not allow external access** to ElastiCache servers, so they can only be used when your ServiceStack application is deployed.

First, you'll need to install `ServiceStack.Caching.Memcached`.

![Install ServiceStack.Caching.Memcached](https://github.com/ServiceStack/Assets/raw/master/img/aws/nuget-install-memcached.png)

To access the Memcached nodes from your `Service`s, you will need to register a `MemcachedClientCache` as a `ICacheClient` with the IoC container. This client has to initialized with each of the node endpoints provided by AWS. From the [ElastiCache Dashboard](https://console.aws.amazon.com/elasticache/home), click on the `nodes` on your cluster to see the node endpoint URLs. 

![Memcached cluster view from Dashboard](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticcache-memcached-nodes.png)

This will show all the nodes in the cluster. For example.

![Listed node endpoints](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticcache-memcached-node-urls.png)

Below is a simple example of a configured self hosting AppHost that uses ElastiCache for caching when deployed and an in memory caching when developing locally.

``` csharp
public class AppHost : AppSelfHostBase
{
    public AppHost() : base("AWS ElastiCache Example", typeof(AppHost).Assembly) {}

    public override void Configure(Container container)
    {
        if (AppSettings.GetString("Environment") == "Production")
        {
			container.Register<ICacheClient>(new MemcachedClientCache(
			    new[]
			    {
			        "memcached-cluster.jbnmsd.0001.apse2.cache.amazonaws.com",
			        "memcached-cluster.jbnmsd.0002.apse2.cache.amazonaws.com",
			        "memcached-cluster.jbnmsd.0003.apse2.cache.amazonaws.com"
			    }));
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
