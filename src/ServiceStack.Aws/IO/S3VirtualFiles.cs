using Amazon.S3;
using ServiceStack.Aws.S3;

namespace ServiceStack.IO
{
    public class S3VirtualFiles : S3VirtualPathProvider
    {
        public S3VirtualFiles(IAmazonS3 client, string bucketName) : base(client, bucketName) {}
    }
}