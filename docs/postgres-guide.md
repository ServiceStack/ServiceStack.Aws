# Getting started with AWS RDS PostgreSQL and OrmLite
ServiceStack.OrmLite library has support for use with PostgreSQL database via the `ServiceStack.OrmLite.PostgreSQL` NuGet package.

To get started, first you will need to create your PostgreSQL database via the AWS RDS service.

## Creating a PostgreSQL RDS Instance

1. Login to the [AWS Web console](https://console.aws.amazon.com/console/home).
2. Select RDS from the ![Services](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/aws-services-menu.png) Menu.
3. Select `Instances` from the left menu.
4. Click ![Launch DB Instance](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/launch-db-button.png)

The above steps will start the RDS Wizard to launch a new DB instance. To setup a new PostgreSQL instance, follow the wizard selecting the appropriate options for your application. As an example, we can create a `Customers` database for a non-production environment.

- **Select Engine** - Select PostgreSQL
- **Production?** - Select `No` for multi-instance/production setup
- **Specify DB Details** - Create a `db.t2.micro` instance with default settings

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/postgres-default-details.png)

- Specify `DB Instance Identifier`, eg `servicestack-example-customers`.
- Specify `Master Username`, eg `postgres`.
- Create and confirm master user password.

- **Configure Advanced Settings** - Leave the suggested settings and specify a database name, eg `customers`. This will be used in your connection string.

> Note: Problems can occure if your default VPC is not setup to DNS Resolution and/or DNS Hostname. Navigate to `Services`, `VPC` and enable these two options on your default VPC.

Click ![Launch DB Instance](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/launch-db-button.png) at the bottom right to launch your new instance. If all is successful, you should see the following.

![](https://github.com/ServiceStack/Assets/raw/master/img/aws/postgres-success.png)

## Connecting with ServiceStack.OrmLite
Now that you're PostgreSQL instance is running, connecting with OrmLite will require the `ServiceStack.OrmLite.PostgreSQL` NuGet package.

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/nuget-install-postgres.png)
>`Install-Package ServiceStack.OrmLite.PostgreSQL`

You'll also need your connection string settings. To find your instance `Endpoint`, select your running DB instances in the RDS console. This will show all the details of your instance including the endpoint address at the top.

In the example below, we are loading our database connection settings from a local `appsettings.txt` file to populate the AppHost's `AppSettings`. For a SelfHost application, the settings can be loaded in the constructor using the following snippet.
``` csharp
var customSettings = new FileInfo(@"~/../appsettings.txt".MapHostAbsolutePath());
            AppSettings = customSettings.Exists
                ? (IAppSettings)new TextFileSettings(customSettings.FullName)
                : new AppSettings();
```

The `appsettings.txt` file in the main project directory contains the following contents.
``` txt
# Settings
Debug False
DbHost {YourEndpointAddress}
DbPort 5432
DbUserName {YourDbUserName}
DbPassword {YourDbPassword}
```

Once this dependency is install and settings ready to use, the `OrmLiteConnectionFactory` can be used with the `PostgreSqlDialect.Provider` to open connections to your RDS instance. For example, we can create a simple AppHost and CRUD service for our customers database.

``` csharp
public class AppHost : AppSelfHostBase
{
    /// <summary>
    /// Default constructor.
    /// Base constructor requires a name and assembly to locate web service classes. 
    /// </summary>
    public AppHost()
        : base("AWSPostgresCustomer", typeof(AppHost).Assembly)
    {
        var customSettings = new FileInfo(@"~/../appsettings.txt".MapHostAbsolutePath());
        AppSettings = customSettings.Exists
            ? (IAppSettings)new TextFileSettings(customSettings.FullName)
            : new AppSettings();
    }

    public override void Configure(Container container)
    {
        container.Register<IDbConnectionFactory>(new OrmLiteConnectionFactory(
            "User ID={0};Password={1};Host={2};Port={3};"
            .Fmt(
                AppSettings.GetString("DbUserName"),
                AppSettings.GetString("DbPassword"),
                AppSettings.GetString("DbHost"),
                AppSettings.GetString("DbPort")),
            PostgreSqlDialect.Provider));

        using (var db = container.Resolve<IDbConnectionFactory>().OpenDbConnection())
        {
            db.CreateTableIfNotExists<Customer>();
        }
    }
}

public class Customer
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; }
}

[Route("/customers", "GET")]
public class GetCustomers : IReturn<GetCustomersResponse> { }

public class GetCustomersResponse
{
    public List<Customer> Results { get; set; }
}

[Route("/customers/{Id}", "GET")]
public class GetCustomer : IReturn<Customer>
{
    public int Id { get; set; }
}

[Route("/customers", "POST")]
public class CreateCustomer : IReturn<Customer>
{
    public string Name { get; set; }
}

[Route("/customers/{Id}", "PUT")]
public class UpdateCustomer : IReturn<Customer>
{
    public int Id { get; set; }

    public string Name { get; set; }
}

[Route("/customers/{Id}", "DELETE")]
public class DeleteCustomer : IReturnVoid
{
    public int Id { get; set; }
}

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