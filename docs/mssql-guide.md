## Getting Started with AWS RDS SQL Server and OrmLite

![](https://github.com/ServiceStack/Assets/raw/master/img/aws/rds-sqlserver-powered-by-aws.png)

ServiceStack.OrmLite library has support for use with a [Microsoft SQL Server](http://www.microsoft.com/en-au/server-cloud/products/sql-server/) database via the [`ServiceStack.OrmLite.SqlServer`](https://www.nuget.org/packages/ServiceStack.OrmLite.SqlServer/) NuGet package. This can be used in conjunction with Amazon's RDS service using SQL Server.

To get started, first you will need to create your SQL Server database via the AWS RDS service.

## Creating a SQL Server RDS Instance

1. Login to the [AWS Web console](https://console.aws.amazon.com/console/home).
2. Select [RDS](https://console.aws.amazon.com/rds/home) from the **Services** from the top menu.
![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/aws-rds-menu.png)
3. Select **Instances** from the **RDS Dashboard** and click **Launch DB Instance**.
![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/launch-db-dashboard.png)

The above steps will start the RDS Wizard to launch a new DB instance. To setup a new SQL Server instance, follow the wizard selecting the appropriate options for your application. As an example, we can create a `Customers` database for a non-production environment.

- **Select Engine**
    - Select **SQL Server**
    - Select appropriate SQL Server version, for this example, **SQL Server SE** 
- **Specify DB Details** 
    - Select **License Model** `license-included` 
    - Create a `db.m1.small` instance with default settings by changing the **DB Instance Class**.

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/mssql-default-details.png)

- Specify **DB Instance Identifier**, eg `customers`.
- Specify **Master Username**, eg `admin`.
- Create and confirm master user password.

- **Configure Advanced Settings** - Leave the suggested settings which will create your RDS instance with network rule that restricts public access via your current public IP address.

> Note: Problems can occure if your default VPC is not setup to DNS Resolution and/or DNS Hostname. Navigate to **Services**, **VPC** and enable these two options on your default VPC.

Click **Launch DB Instance** at the *bottom right* to launch your new instance. If all is successful, you should see the following.

![](https://github.com/ServiceStack/Assets/raw/master/img/aws/create-db-success.png)

## Connecting with ServiceStack.OrmLite
Now that you're SQL Server instance is running, connecting with OrmLite will require the `ServiceStack.OrmLite.SqlServer` NuGet package as well as connection string to your new SQL Server instance.
> If you are connecting to a new instance without a database, you'll need to create a new Database via SQL Management Studio first. For this example the `customers` database was created.

``` xml
<appSettings>
    <add key="ConnectionString" value="Data Source={Endpoint},{Port};Initial Catalog=customers;User ID={User};Password={Password}" />   
</appSettings>
```
![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/nuget-install-mssql.png)

Once this dependency is installed, the `OrmLiteConnectionFactory` can be used with the `SqlServerDialect.Provider` can be configured in the AppHost Configure method. For example.

``` csharp
public class AppHost : AppSelfHostBase
{
    public AppHost() : base("AWS SQL Server Customers", typeof(AppHost).Assembly) {}

    public override void Configure(Container container)
    {
        container.Register<IDbConnectionFactory>(c => new OrmLiteConnectionFactory(
            AppSettings.GetString("ConnectionString"), SqlServerDialect.Provider));

        using (var db = container.Resolve<IDbConnectionFactory>().Open())
        {
            if (db.CreateTableIfNotExists<Customer>())
            {
                //Add seed data
            }
        }
    }
}

```

Using our connection from a ServiceStack Service, we can use the `Db` property to access our `Customer` table. Eg, Below is an example of a CRUD service using OrmLite.

``` csharp
public class CustomerService : Service
{
    public object Get(GetCustomers request)
    {
        return new GetCustomersResponse { Results = Db.Select<Customer>() };
    }

    public object Get(GetCustomer request)
    {
        return Db.SingleById<Customer>(request.Id);
    }

    public object Post(CreateCustomer request)
    {
        var customer = new Customer { Name = request.Name };
        Db.Save(customer);
        return customer;
    }

    public object Put(UpdateCustomer request)
    {
        var customer = Db.SingleById<Customer>(request.Id);
        if (customer == null)
            throw HttpError.NotFound("Customer '{0}' does not exist".Fmt(request.Id));

        customer.Name = request.Name;
        Db.Update(customer);

        return customer;
    }

    public void Delete(DeleteCustomer request)
    {
        Db.DeleteById<Customer>(request.Id);
    }
}
```

See the [OrmLite GitHub](https://github.com/ServiceStack/ServiceStack.OrmLite#api-examples) page for more info on working with OrmLite API.
