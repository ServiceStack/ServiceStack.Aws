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
using Todos;

namespace AwsApps
{
    public class AppHost : AppHostBase
    {
        public AppHost() : base("AWS Examples", typeof(AppHost).Assembly) { }

        public override void Configure(Container container)
        {
            JsConfig.EmitCamelCaseNames = true;

            //Comment out 2 lines below to change to use local FileSystem instead of S3
            var s3Client = new AmazonS3Client(AwsConfig.AwsAccessKey, AwsConfig.AwsSecretKey, RegionEndpoint.USEast1);
            VirtualFileSystem = new S3VirtualPathProvider(s3Client, AwsConfig.S3BucketName, this);

            container.Register<IPocoDynamo>(c => new PocoDynamo(AwsConfig.CreateAmazonDynamoDb()));
            var db = container.Resolve<IPocoDynamo>();
            db.RegisterTable<Todo>();
            db.InitSchema();

            Plugins.Add(new RazorFormat());
        }

        public override List<IVirtualPathProvider> GetVirtualPathProviders()
        {
            var pathProviders = base.GetVirtualPathProviders();
            pathProviders.Add(VirtualFileSystem);
            return pathProviders;
        }
    }
}