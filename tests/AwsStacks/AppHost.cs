using System.Collections.Generic;
using Amazon;
using Amazon.S3;
using Funq;
using ServiceStack;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Aws.S3;
using ServiceStack.IO;
using ServiceStack.Razor;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
using Todos;

namespace AwsStacks
{
    public class AppHost : AppHostBase
    {
        public AppHost() : base("AWS Examples", typeof(TodoService).Assembly) { }

        public override void Configure(Container container)
        {
            JsConfig.EmitCamelCaseNames = true;

            var s3Client = new AmazonS3Client(AwsConfig.AwsAccessKey, AwsConfig.AwsSecretKey, RegionEndpoint.USEast1);
            container.Register<IWriteableVirtualPathProvider>(c =>
                new S3VirtualPathProvider(s3Client, AwsConfig.S3BucketName, this));

            container.Register<IPocoDynamo>(c => new PocoDynamo(AwsConfig.CreateAmazonDynamoDb()));
            var db = container.Resolve<IPocoDynamo>();
            db.RegisterTable<Todo>();
            db.InitSchema();

            Plugins.Add(new RazorFormat());
        }

        public override List<IVirtualPathProvider> GetVirtualPathProviders()
        {
            var pathProviders = base.GetVirtualPathProviders();
            pathProviders.Add(Container.Resolve<IWriteableVirtualPathProvider>());
            return pathProviders;
        }
    }
}