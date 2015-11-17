ServiceStack.AmazonWebServices
==============================

## AWS's servicified platform and polyglot ecosystem

By building their managed platform behind platform-agnostic web services, Amazon have largely eroded this barrier. We
can finally tap into the same ecosystem [innovative Startups are using](http://techstacks.io/tech/amazon-ec2) with
nothing more than the complexity cost of a service call - the required effort even further reduced with native clients. 
Designing its services behind message-based APIs made it much easier for Amazon to enable a new polyglot world with 
[native clients for most popular platforms](https://aws.amazon.com/dynamodb/developer-resources/#SDK), putting .NET
on a level playing field with other platforms thanks to [AWS SDK for .NET's](http://aws.amazon.com/sdk-for-net/) 
well-maintained typed native clients. By providing its functionality behind well-defined services, for the first time 
we've seen in a long time, .NET developers are able to benefit from this new polyglot world where solutions and app 
logic written in other languages can be easily translated into .NET languages - a trait which has been invaluable whilst 
developing ServiceStack's integration support for AWS.

This also means features and improvements to reliability, performance and scalability added to its back-end servers benefit 
every language and ecosystem using them. .NET developers are no longer at a disadvantage and can now leverage the same 
platform Hacker Communities and next wave of technology leading Startups are built on, benefiting from the Tech Startup 
culture of sharing their knowledge and experiences and pushing the limits of what's possible today.

AWS offers unprecedented productivity for back-end developers, its servicified hardware and infrastructure encapsulates 
the complexity of managing servers at a high-level programmatic abstraction that's effortless to consume and automate. 
These productivity gains is why we've been running our public servers on AWS for more than 2 years. The vast array of 
services on offer means we have everything our solutions need within the AWS Console, our RDS managed PostgreSQL databases 
takes care of automated backups and software updates, ease of snapshots means we can encapsulate and backup the 
configuration of our servers and easily spawn new instances. AWS has made software developers more capable than ever, 
and with its first-class native client support leveling the playing field for .NET, there's no reason why 
[the next Instagram](http://highscalability.com/blog/2012/4/9/the-instagram-architecture-facebook-bought-for-a-cool-billio.html)
couldn't be built by a small team of talented .NET developers.

## ServiceStack + Amazon Web Services

We're excited to participate in AWS's vibrant ecosystem and provide first-class support and deep integration with AWS where 
ServiceStack's decoupled substitutable functionality now seamlessly integrates with popular AWS back-end technologies. 
It's now more productive than ever to develop and host ServiceStack solutions entirely on the managed AWS platform!

## ServiceStack.Aws

All of ServiceStack's support for AWS is encapsulated within the single **ServiceStack.Aws** NuGet package which 
references the latest modular AWSSDK **v3.1x** dependencies **.NET 4.5+** projects can install from NuGet with:

    PM> Install-Package ServiceStack.Aws

This **ServiceStack.Aws** NuGet package includes implementations for the following ServiceStack providers:

  - **[PocoDynamo](#pocodynamo)** - Exciting new declarative, code-first POCO client for DynamoDB with LINQ support
  - **[SqsMqServer](#sqsmqserver)** - A new [MQ Server](https://github.com/ServiceStack/ServiceStack/wiki/Messaging) for invoking ServiceStack Services via Amazon SQS MQ Service
  - **[S3VirtualPathProvider](#S3virtualpathprovider)** - A read/write [Virtual FileSystem](https://github.com/ServiceStack/ServiceStack/wiki/Virtual-file-system) around Amazon's S3 Simple Storage Service
  - **[DynamoDbAuthRepository](#dynamodbauthrepository)** - A new [UserAuth repository](https://github.com/ServiceStack/ServiceStack/wiki/Authentication-and-authorization) storing UserAuth info in DynamoDB
  - **[DynamoDbAppSettings](#dynamodbappsettings)** - An [AppSettings provider](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings) storing App configuration in DynamoDB
  - **[DynamoDbCacheClient](#dynamodbcacheclient)** - A new [Caching Provider](https://github.com/ServiceStack/ServiceStack/wiki/Caching) for DynamoDB

> We'd like to give a big thanks to [Chad Boyd](https://github.com/boydc7) from Spruce Media for contributing the SqsMqServer implementation.

## [AWS Live Examples](http://awsapps.servicestack.net/)

To demonstrate the ease of which you can build AWS-powered solutions with ServiceStack we've rewritten 6 of our existing 
[Live Demos](https://github.com/ServiceStackApps/LiveDemos) to use a pure AWS managed backend using:

 - [Amazon DynamoDB](https://aws.amazon.com/dynamodb/) for data persistance
 - [Amazon S3](https://aws.amazon.com/s3/) for file storage
 - [Amazon SQS](https://aws.amazon.com/sqs/) for background processing of MQ requests 
 - [Amazon SES](https://aws.amazon.com/ses/) for sending emails
 
[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/awsapps.png)](http://awsapps.servicestack.net/)

### Simple AppHost Configuration

A good indication showing how simple it is to build ServiceStack + AWS solutions is the size of the 
[AppHost](https://github.com/ServiceStackApps/AwsApps/blob/master/src/AwsApps/AppHost.cs) which contains all the 
configuration for **5 different Apps** below utilizing all the AWS technologies listed above contained within a **single** 
ASP.NET Web Application where each application's UI and back-end Service implementation are encapsulated under 
their respective sub directories:

  - [/awsath](https://github.com/ServiceStackApps/AwsApps/tree/master/src/AwsApps/awsauth) -> [awsapps.servicestack.net/awsauth/](http://awsapps.servicestack.net/awsauth/)
  - [/emailcontacts](https://github.com/ServiceStackApps/AwsApps/tree/master/src/AwsApps/emailcontacts) -> [awsapps.servicestack.net/emailcontacts/](http://awsapps.servicestack.net/emailcontacts/)
  - [/imgur](https://github.com/ServiceStackApps/AwsApps/tree/master/src/AwsApps/imgur) -> [awsapps.servicestack.net/imgur/](http://awsapps.servicestack.net/imgur/)
  - [/restfiles](https://github.com/ServiceStackApps/AwsApps/tree/master/src/AwsApps/restfiles) -> [awsapps.servicestack.net/restfiles/](http://awsapps.servicestack.net/restfiles/)
  - [/todo](https://github.com/ServiceStackApps/AwsApps/tree/master/src/AwsApps/todo) -> [awsapps.servicestack.net/todo/](http://awsapps.servicestack.net/todo/)

## [AWS Razor Rockstars](http://awsrazor.servicestack.net/)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/awsrazor.png)](http://awsrazor.servicestack.net/)

The 
[implementation for AWS Razor Rockstars](https://github.com/ServiceStackApps/RazorRockstars/tree/master/src/RazorRockstars.S3) 
is kept with all the other ports of Razor Rockstars in the [RazorRockstars repository](https://github.com/ServiceStackApps/RazorRockstars).
The main difference that stands out with [RazorRockstars.S3](https://github.com/ServiceStackApps/RazorRockstars/tree/master/src/RazorRockstars.S3)
is that all the content for the App is **not** contained within project as all its Razor Views, Markdown Content, imgs, 
js, css, etc. are instead being served **directly from an S3 Bucket** :) 

This is simply enabled by overriding `GetVirtualFileSources()` and adding the new 
`S3VirtualPathProvider` to the list of file sources:

```csharp
public class AppHost : AppHostBase
{
    public override void Configure(Container container)
    {
        //All Razor Views, Markdown Content, imgs, js, css, etc are served from an S3 Bucket
        var s3 = new AmazonS3Client(AwsConfig.AwsAccessKey, AwsConfig.AwsSecretKey, RegionEndpoint.USEast1);
        VirtualFiles = new S3VirtualPathProvider(s3, AwsConfig.S3BucketName, this);
    }
    
    public override List<IVirtualPathProvider> GetVirtualFileSources()
    {
        //Add S3 Bucket as lowest priority Virtual Path Provider 
        var pathProviders = base.GetVirtualFileSources();
        pathProviders.Add(VirtualFiles);
        return pathProviders;
    }
}
```

The code to import RazorRockstars content into an S3 bucket is trivial: we just use a local FileSystem provider to get 
all the files we're interested in from the main ASP.NET RazorRockstars projects folder, then write them to the configured 
S3 VirtualFiles Provider:

```csharp
var s3Client = new AmazonS3Client(AwsConfig.AwsAccessKey, AwsConfig.AwsSecretKey, RegionEndpoint.USEast1);
var s3 = new S3VirtualPathProvider(s3Client, AwsConfig.S3BucketName, appHost);
            
var fs = new FileSystemVirtualPathProvider(appHost, "~/../RazorRockstars.WebHost".MapHostAbsolutePath());

var skipDirs = new[] { "bin", "obj" };
var matchingFileTypes = new[] { "cshtml", "md", "css", "js", "png", "jpg" };
//Update links to reference the new S3 AppHost.cs + RockstarsService.cs source code
var replaceHtmlTokens = new Dictionary<string, string> {  
    { "title-bg.png", "title-bg-aws.png" }, //S3 Title Background
    { "https://gist.github.com/3617557.js", "https://gist.github.com/mythz/396dbf54ce6079cc8b2d.js" },
    { "https://gist.github.com/3616766.js", "https://gist.github.com/mythz/ca524426715191b8059d.js" },
    { "RazorRockstars.WebHost/RockstarsService.cs", "RazorRockstars.S3/RockstarsService.cs" },        
};

foreach (var file in fs.GetAllFiles())
{
    if (skipDirs.Any(x => file.VirtualPath.StartsWith(x))) continue;
    if (!matchingFileTypes.Contains(file.Extension)) continue;

    if (file.Extension == "cshtml")
    {
        var html = file.ReadAllText();
        replaceHtmlTokens.Each(x => html = html.Replace(x.Key, x.Value));
        s3.WriteFile(file.VirtualPath, html);
    }
    else
    {
        s3.WriteFile(file);
    }
}
```

During the import we also update the links in the Razor `*.cshtml` pages to reference the new RazorRockstars.S3 content.

### Update S3 Bucket to enable LiveReload of Razor Views and Markdown

Another nice feature of having all content maintained in an S3 Bucket is that you can just change files in the S3 Bucket 
directly and have all App Servers immediately reload the Razor Views, Markdown content and static resources without redeploying. 

#### CheckLastModifiedForChanges

To enable this feature we just tell the Razor and Markdown plugins to check the source file for changes before displaying each page:

```csharp
GetPlugin<MarkdownFormat>().CheckLastModifiedForChanges = true;
Plugins.Add(new RazorFormat { CheckLastModifiedForChanges = true });
```

When this is enabled the View Engines checks the ETag of the source file to find out if it's changed, if it did,
it will rebuild and replace it with the new view before rendering it. 
Given [S3 supports object versioning](http://docs.aws.amazon.com/AmazonS3/latest/dev/Versioning.html) this feature
should enable a new class use-cases for developing Content Heavy management sites with ServiceStack.

#### Explicit RefreshPage

One drawback of enabling `CheckLastModifiedForChanges` is that it forces a remote S3 call for each view before rendering it.
A more efficient approach is to instead notify the App Servers which files have changed so they can reload them once,
alleviating the need for multiple ETag checks at runtime, which is the approach we've taken with the 
[UpdateS3 Service](https://github.com/ServiceStackApps/RazorRockstars/blob/e159bb9d2e27eba7fc1a9ce1822b479602de8e0f/src/RazorRockstars.S3/RockstarsService.cs#L139):

```csharp
if (request.Razor)
{
    var kurtRazor = VirtualFiles.GetFile("stars/dead/cobain/default.cshtml");
    VirtualFiles.WriteFile(kurtRazor.VirtualPath, 
        UpdateContent("UPDATED RAZOR", kurtRazor.ReadAllText(), request.Clear));
    HostContext.GetPlugin<RazorFormat>().RefreshPage(kurtRazor.VirtualPath); //Force reload of Razor View
}

var kurtMarkdown = VirtualFiles.GetFile("stars/dead/cobain/Content.md");
VirtualFiles.WriteFile(kurtMarkdown.VirtualPath, 
    UpdateContent("UPDATED MARKDOWN", kurtMarkdown.ReadAllText(), request.Clear));
HostContext.GetPlugin<MarkdownFormat>().RefreshPage(kurtMarkdown.VirtualPath); //Force reload of Markdown
```

#### Live Reload Demo

You can test live reloading of the above Service with the routes below which modify Markdown and Razor views with the
current time:

  - [/updateS3](http://awsrazor.servicestack.net/updateS3) - Update Markdown Content
  - [/updateS3?razor=true](http://awsrazor.servicestack.net/updateS3?razor=true) - Update Razor View
  - [/updateS3?razor=true&clear=true](http://awsrazor.servicestack.net/updateS3?razor=true&clear=true) - Revert changes
  
> This forces a recompile of the modified views which greatly benefits from a fast CPU and is a bit slow on our 
Live Demos server that's running on a **m1.small** instance shared with 25 other ASP.NET Web Applications. 

## [AWS Imgur](http://awsapps.servicestack.net/imgur/)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/imgur.png)](http://awsapps.servicestack.net/imgur/)

### S3VirtualPathProvider 

The backend 
[ImageService.cs](https://github.com/ServiceStackApps/AwsApps/blob/master/src/AwsApps/imgur/ImageService.cs) 
implementation for AWS Imgur has been rewritten to use the Virtual FileSystem instead of 
[accessing the FileSystem directly](https://github.com/ServiceStackApps/Imgur/blob/master/src/Imgur/Global.asax.cs).
The benefits of this approach is that with 
[2 lines of configuration](https://github.com/ServiceStackApps/AwsApps/blob/4817f5c6ad69defd74d528403bfdb03e5958b0b3/src/AwsApps/AppHost.cs#L44-L45)
we can have files written to an S3 Bucket instead:

```csharp
var s3Client = new AmazonS3Client(AwsConfig.AwsAccessKey, AwsConfig.AwsSecretKey, RegionEndpoint.USEast1);
VirtualFiles = new S3VirtualPathProvider(s3Client, AwsConfig.S3BucketName, this);
```

If we comment out the above configuration any saved files are instead written to the local FileSystem (default).

The benefit of using managed S3 File Storage is better scalability as your App Servers can remain stateless, improved
performance as overhead of serving static assets can be offloaded by referencing the S3 Bucket directly and for even 
better responsiveness you can connect the S3 bucket to a CDN.

## [REST Files](http://awsapps.servicestack.net/restfiles/)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/restfiles.png)](http://awsapps.servicestack.net/restfiles/)

REST Files GitHub-like explorer is another example that was 
[rewritten to use ServiceStack's Virtual File System](https://github.com/ServiceStackApps/AwsApps/blob/master/src/AwsApps/restfiles/FilesService.cs)
and now provides remote file management of an S3 Bucket behind a REST-ful API.

## [AWS Email Contacts](http://awsapps.servicestack.net/emailcontacts/)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/emailcontacts.png)](http://awsapps.servicestack.net/emailcontacts/)

### SqsMqServer

The [AWS Email Contacts](http://awsapps.servicestack.net/emailcontacts/) example shows the same long-running 
[EmailContact Service](https://github.com/ServiceStackApps/AwsApps/blob/4817f5c6ad69defd74d528403bfdb03e5958b0b3/src/AwsApps/emailcontacts/EmailContactServices.cs#L81)
being executed from both HTTP and MQ Server by just 
[changing which url the HTML Form is posted to](https://github.com/ServiceStackApps/AwsApps/blob/4817f5c6ad69defd74d528403bfdb03e5958b0b3/src/AwsApps/emailcontacts/default.cshtml#L203):

```html
//html
<form id="form-emailcontact" method="POST"
    action="@(new EmailContact().ToPostUrl())" 
    data-action-alt="@(new EmailContact().ToOneWayUrl())">
    ...
    <div>
        <input type="checkbox" id="chkAction" data-click="toggleAction" />
        <label for="chkAction">Email via MQ</label>
    </div>
    ...   
</form>
```

> The urls are populated from a typed Request DTO using the [Reverse Routing Extension methods](https://github.com/ServiceStack/ServiceStack/wiki/Routing#reverse-routing)

Checking the **Email via MQ** checkbox fires the JavaScript handler below that's registered as [declarative event in ss-utils.js](https://github.com/ServiceStack/ServiceStack/wiki/ss-utils.js-JavaScript-Client-Library#declarative-events):

```js
$(document).bindHandlers({
    toggleAction: function() {
        var $form = $(this).closest("form"), action = $form.attr("action");
        $form.attr("action", $form.data("action-alt"))
                .data("action-alt", action);
    }
});
```

The code to configure and start an SQS MQ Server is similar to [other MQ Servers](https://github.com/ServiceStack/ServiceStack/wiki/Messaging): 

```csharp
container.Register<IMessageService>(c => new SqsMqServer(
    AwsConfig.AwsAccessKey, AwsConfig.AwsSecretKey, RegionEndpoint.USEast1) {
    DisableBuffering = true, // Trade-off latency vs efficiency
});

var mqServer = container.Resolve<IMessageService>();
mqServer.RegisterHandler<EmailContacts.EmailContact>(ExecuteMessage);
mqServer.Start();
```

When an MQ Server is registered, ServiceStack automatically publishes Requests accepted on the "One Way" 
[pre-defined route](https://github.com/ServiceStack/ServiceStack/wiki/Routing#pre-defined-routes)
to the registered MQ broker. The message is later picked up and executed by a Message Handler on a background Thread.

## [AWS Auth](http://awsapps.servicestack.net/awsauth/)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/awsauth.png)](http://awsapps.servicestack.net/awsauth/)

### DynamoDbAuthRepository

[AWS Auth](http://awsapps.servicestack.net/awsauth/) 
is an example showing how easy it is to enable multiple Auth Providers within the same App which allows Sign-Ins from 
Twitter, Facebook, GitHub, Google, Yahoo and LinkedIn OAuth providers, as well as HTTP Basic and Digest Auth and 
normal Registered User logins and Custom User Roles validation, all managed in DynamoDB Tables using 
the registered `DynamoDbAuthRepository` below: 

```csharp
container.Register<IAuthRepository>(new DynamoDbAuthRepository(db, initSchema:true));
```

Standard registration code is used to configure the `AuthFeature` with all the different Auth Providers AWS Auth wants 
to support:

```csharp
return new AuthFeature(() => new AuthUserSession(),
    new IAuthProvider[]
    {
        new CredentialsAuthProvider(),              //HTML Form post of UserName/Password credentials
        new BasicAuthProvider(),                    //Sign-in with HTTP Basic Auth
        new DigestAuthProvider(AppSettings),        //Sign-in with HTTP Digest Auth
        new TwitterAuthProvider(AppSettings),       //Sign-in with Twitter
        new FacebookAuthProvider(AppSettings),      //Sign-in with Facebook
        new YahooOpenIdOAuthProvider(AppSettings),  //Sign-in with Yahoo OpenId
        new OpenIdOAuthProvider(AppSettings),       //Sign-in with Custom OpenId
        new GoogleOAuth2Provider(AppSettings),      //Sign-in with Google OAuth2 Provider
        new LinkedInOAuth2Provider(AppSettings),    //Sign-in with LinkedIn OAuth2 Provider
        new GithubAuthProvider(AppSettings),        //Sign-in with GitHub OAuth Provider
    })
{
    HtmlRedirect = "/awsauth/",                     //Redirect back to AWS Auth app after OAuth sign in
    IncludeRegistrationService = true,              //Include ServiceStack's built-in RegisterService
};
```

### DynamoDbAppSettings

The AuthFeature looks for the OAuth settings for each AuthProvider in the registered
[AppSettings](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings), which for deployed **Release** builds 
gets them from multiple sources. Since `DynamoDbAppSettings` is registered first in a `MultiAppSettings` collection
it checks entries in the DynamoDB `ConfigSetting` Table first before falling back to local 
[Web.config appSettings](https://github.com/ServiceStackApps/AwsApps/blob/4817f5c6ad69defd74d528403bfdb03e5958b0b3/src/AwsApps/Web.config#L15): 

```csharp
#if !DEBUG
    AppSettings = new MultiAppSettings(
        new DynamoDbAppSettings(new PocoDynamo(AwsConfig.CreateAmazonDynamoDb()), initSchema:true),
        new AppSettings()); // fallback to Web.confg
#endif
```

Storing production config in DynamoDB reduces the effort for maintaining production settings decoupled from source code. 
The App Settings were populated in DynamoDB using
[this simple script](https://github.com/ServiceStackApps/AwsApps/blob/9d4d3c3dfbf127ce0890d0984c264e8b440abd3f/src/AwsApps/AdminTasks.cs#L58)
which imports its settings from a local [appsettings.txt file](https://github.com/ServiceStack/ServiceStack/wiki/AppSettings#textfilesettings):

```csharp
var fileSettings = new TextFileSettings("~/../../deploy/appsettings.txt".MapHostAbsolutePath());
var dynamoSettings = new DynamoDbAppSettings(AwsConfig.CreatePocoDynamo());
dynamoSettings.InitSchema();

//dynamoSettings.Set("SmtpConfig", "{Username:REPLACE_USER,Password:REPLACE_PASS,Host:AWS_HOST,Port:587}");
foreach (var config in fileSettings.GetAll())
{
    dynamoSettings.Set(config.Key, config.Value);
}
```

#### ConfigSettings Table in DynamoDB

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/release-notes/aws-configsettings.png)

## [AWS Todos](http://awsapps.servicestack.net/todo/)

[![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/apps/screenshots/todos.png)](http://awsapps.servicestack.net/todo/)

The [Backbone TODO App](http://todomvc.com/examples/backbone/) is a famous minimal example used as a "Hello, World" 
example to showcase and compare JavaScript client frameworks. The example also serves as a good illustration of the 
clean and minimal code it takes to build a simple CRUD Service utilizing a DynamoDB back-end with the new PocoDynamo client:

```csharp
public class TodoService : Service
{
    public IPocoDynamo Dynamo { get; set; }

    public object Get(Todo todo)
    {
        if (todo.Id != default(long))
            return Dynamo.GetItem<Todo>(todo.Id);

        return Dynamo.GetAll<Todo>();
    }

    public Todo Post(Todo todo)
    {
        Dynamo.PutItem(todo);
        return todo;
    }

    public Todo Put(Todo todo)
    {
        return Post(todo);
    }

    public void Delete(Todo todo)
    {
        Dynamo.DeleteItem<Todo>(todo.Id);
    }
}
```

As it's a clean POCO, the `Todo` model can be also reused as-is throughout ServiceStack in Redis, OrmLite, Caching, Config, DTO's, etc:

```csharp
public class Todo
{
    [AutoIncrement]
    public long Id { get; set; }
    public string Content { get; set; }
    public int Order { get; set; }
    public bool Done { get; set; }
}
```

## [PocoDynamo](https://github.com/ServiceStack/PocoDynamo)

PocoDynamo is a highly productive, feature-rich, typed .NET client which extends 
[ServiceStack's Simple POCO life](http://stackoverflow.com/a/32940275/85785) 
by enabling re-use of your code-first data models with Amazon's industrial strength and highly-scalable 
NoSQL [DynamoDB](https://aws.amazon.com/dynamodb/).

![](https://raw.githubusercontent.com/ServiceStack/Assets/master/img/aws/pocodynamo/related-customer.png)

#### First class support for reusable, code-first POCOs

It works conceptually similar to ServiceStack's other code-first
[OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite) and 
[Redis](https://github.com/ServiceStack/ServiceStack.Redis) clients by providing a high-fidelity, managed client that enhances
AWSSDK's low-level [IAmazonDynamoDB client](http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/UsingAWSsdkForDotNet.html), 
with rich, native support for intuitively mapping your re-usable code-first POCO Data models into 
[DynamoDB Data Types](http://docs.aws.amazon.com/amazondynamodb/latest/APIReference/API_Types.html). 

### PocoDynamo Features

#### Advanced idiomatic .NET client

PocoDynamo provides an idiomatic API that leverages .NET advanced language features with streaming API's returning
`IEnumerable<T>` lazily evaluated responses that transparently performs multi-paged requests behind-the-scenes as the 
result set is iterated. It high-level API's provides a clean lightweight adapter to
transparently map between .NET built-in data types and DynamoDB's low-level attribute values. Its efficient batched 
API's take advantage of DynamoDB's `BatchWriteItem` and `BatchGetItem` batch operations to perform the minimum number 
of requests required to implement each API.

#### Typed, LINQ provider for Query and Scan Operations

PocoDynamo also provides rich, typed LINQ-like querying support for constructing DynamoDB Query and Scan operations, 
dramatically reducing the effort to query DynamoDB, enhancing readability whilst benefiting from Type safety in .NET. 

#### Declarative Tables and Indexes

Behind the scenes DynamoDB is built on a dynamic schema which whilst open and flexible, can be cumbersome to work with 
directly in typed languages like C#. PocoDynamo bridges the gap and lets your app bind to impl-free and declarative POCO 
data models that provide an ideal high-level abstraction for your business logic, hiding a lot of the complexity of 
working with DynamoDB - dramatically reducing the code and effort required whilst increasing the readability and 
maintainability of your Apps business logic.

It includes optimal support for defining simple local indexes which only require declaratively annotating properties 
to index with an `[Index]` attribute.

Typed POCO Data Models can be used to define more complex Local and Global DynamoDB Indexes by implementing 
`IGlobalIndex<Poco>` or `ILocalIndex<Poco>` interfaces which PocoDynamo uses along with the POCOs class structure 
to construct Table indexes at the same time it creates the tables.

In this way the Type is used as a DSL to define DynamoDB indexes where the definition of the index is decoupled from 
the imperative code required to create and query it, reducing the effort to create them whilst improving the 
visualization and understanding of your DynamoDB architecture which can be inferred at a glance from the POCO's 
Type definition. PocoDynamo also includes first-class support for constructing and querying Global and Local Indexes 
using a familiar, typed LINQ provider.

#### Resilient

Each operation is called within a managed execution which transparently absorbs the variance in cloud services 
reliability with automatic retries of temporary errors, using an exponential backoff as recommended by Amazon. 

#### Enhances existing APIs

PocoDynamo API's are a lightweight layer modeled after DynamoDB API's making it predictable the DynamoDB operations 
each API calls under the hood, retaining your existing knowledge investment in DynamoDB. 
When more flexibility is needed you can access the low-level `AmazonDynamoDBclient from the `IPocoDynamo.DynamoDb` 
property and talk with it directly.

Whilst PocoDynamo doesn't save you for needing to learn DynamoDB, its deep integration with .NET and rich support for 
POCO's smoothes out the impedance mismatches to enable an type-safe, idiomatic, productive development experience.

#### High-level features

PocoDynamo includes its own high-level features to improve the re-usability of your POCO models and the development 
experience of working with DynamoDB with support for Auto Incrementing sequences, Query expression builders, 
auto escaping and converting of Reserved Words to placeholder values, configurable converters, scoped client 
configurations, related items, conventions, aliases, dep-free data annotation attributes and more.

### Download

PocoDynamo is contained in ServiceStack's AWS NuGet package:

    PM> Install-Package ServiceStack.Aws
   
<sub>PocoDynamo has a 10 Tables [free-quota usage](https://servicestack.net/download#free-quotas) limit which is unlocked with a [license key](https://servicestack.net/pricing).</sub>
    
To get started we'll need to create an instance of `AmazonDynamoDBClient` with your AWS credentials and Region info:

```csharp
var awsDb = new AmazonDynamoDBClient(AWS_ACCESS_KEY, AWS_SECRET_KEY, RegionEndpoint.USEast1);
```

Then to create a PocoDynamo client pass the configured AmazonDynamoDBClient instance above:

```csharp
var db = new PocoDynamo(awsDb);
```

> Clients are Thread-Safe so you can register them as a singleton and share the same instance throughout your App

### Creating a Table with PocoDynamo

PocoDynamo enables a declarative code-first approach where it's able to create DynamoDB Table schemas from just your 
POCO class definition. Whilst you could call `db.CreateTable<Todo>()` API and create the Table directly, the recommended 
approach is instead to register all the tables your App uses with PocoDynamo on Startup, then just call `InitSchema()` 
which will go through and create all missing tables:

```csharp
//PocoDynamo
var db = new PocoDynamo(awsDb)
    .RegisterTable<Todo>();

db.InitSchema();

db.GetTableNames().PrintDump();
```

In this way your App ends up in the same state with all tables created if it was started with **no tables**, **all tables** 
or only a **partial list** of tables. After the tables are created we query DynamoDB to dump its entire list of Tables, 
which if you started with an empty DynamoDB instance would print the single **Todo** table name to the Console:

    [
        Todo
    ]

### Managed DynamoDB Client

Every request in PocoDynamo is invoked inside a managed execution where any temporary errors are retried using the 
[AWS recommended retries exponential backoff](http://docs.aws.amazon.com/amazondynamodb/latest/developerguide/ErrorHandling.html#APIRetries).

All PocoDynamo API's returning `IEnumerable<T>` returns a lazy evaluated stream which behind-the-scenes sends multiple
paged requests as needed whilst the sequence is being iterated. As LINQ APIs are also lazily evaluated you could use 
`Take()` to only download however the exact number results you need. So you can query the first 100 table names with:

```csharp
//PocoDynamo
var first100TableNames = db.GetTableNames().Take(100).ToList();
```

and PocoDynamo will only make the minimum number of requests required to fetch the first 100 results.

### PocoDynamo Examples

#### [DynamoDbCacheClient](https://github.com/ServiceStack/ServiceStack.Aws/blob/master/src/ServiceStack.Aws/DynamoDb/DynamoDbCacheClient.cs)

We've been quick to benefit from the productivity advantages of PocoDynamo ourselves where we've used it to rewrite
[DynamoDbCacheClient](https://github.com/ServiceStack/ServiceStack.Aws/blob/master/src/ServiceStack.Aws/DynamoDb/DynamoDbCacheClient.cs)
which is now just 2/3 the size and much easier to maintain than the existing 
[Community-contributed version](https://github.com/ServiceStack/ServiceStack/blob/22aca105d39997a8ea4c9dc20b242f78e07f36e0/src/ServiceStack.Caching.AwsDynamoDb/DynamoDbCacheClient.cs)
whilst at the same time extending it with even more functionality where it now implements the `ICacheClientExtended` API.

#### [DynamoDbAuthRepository](https://github.com/ServiceStack/ServiceStack.Aws/blob/master/src/ServiceStack.Aws/DynamoDb/DynamoDbAuthRepository.cs)

PocoDynamo's code-first Typed API made it much easier to implement value-added DynamoDB functionality like the new
[DynamoDbAuthRepository](https://github.com/ServiceStack/ServiceStack.Aws/blob/master/src/ServiceStack.Aws/DynamoDb/DynamoDbAuthRepository.cs)
which due sharing a similar code-first POCO approach to OrmLite, ended up being a straight-forward port of the existing
[OrmLiteAuthRepository](https://github.com/ServiceStack/ServiceStack/blob/master/src/ServiceStack.Server/Auth/OrmLiteAuthRepository.cs)
where it was able to reuse the existing `UserAuth` and `UserAuthDetails` POCO data models.

#### [DynamoDbTests](https://github.com/ServiceStack/ServiceStack.Aws/tree/master/tests/ServiceStack.Aws.DynamoDbTests)

Despite its young age we've added a comprehensive test suite behind PocoDynamo which has become our exclusive client
for developing DynamoDB-powered Apps.

### [PocoDynamo Docs](https://github.com/ServiceStack/PocoDynamo)

This only scratches the surface of what PocoDynamo can do, comprehensive documentation is available in the 
[PocoDynamo project](https://github.com/ServiceStack/PocoDynamo) explaining how it compares to DynamoDB's AWSSDK client,
how to use it to store related data, how to query indexes and how to use its rich LINQ querying functionality to query
DynamoDB.

## [Getting started with AWS + ServiceStack Guides](https://github.com/ServiceStackApps/AwsGettingStarted)

Amazon offers managed hosting for a number of RDBMS and Caching servers which ServiceStack provides first-class
clients for. We've provided a number of guides to walk through setting up these services from your AWS account 
and connect to them with ServiceStack's typed .NET clients.

### [AWS RDS PostgreSQL and OrmLite](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/postgres-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/rds-postgres-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/postgres-guide.md)

### [AWS RDS Aurora and OrmLite](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/aurora-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/rds-aurora-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/aurora-guide.md)

### [AWS RDS MySQL and OrmLite](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/mssql-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/rds-mysql-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/mssql-guide.md)

### [AWS RDS MariaDB and OrmLite](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/mariadb-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/rds-mariadb-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/mariadb-guide.md)

### [AWS RDS SQL Server and OrmLite](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/mssql-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/rds-sqlserver-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/mssql-guide.md)

### [AWS ElastiCache Redis and ServiceStack](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/redis-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticache-redis-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/redis-guide.md)

### [AWS ElastiCache Redis and ServiceStack](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/memcached-guide.md)

[![](https://github.com/ServiceStack/Assets/raw/master/img/aws/elasticache-memcached-powered-by-aws.png)](https://github.com/ServiceStackApps/AwsGettingStarted/blob/master/docs/memcached-guide.md)

The source code used in each guide is also available in the [AwsGettingStarted](https://github.com/ServiceStackApps/AwsGettingStarted) repo.

