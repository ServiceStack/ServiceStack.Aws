using NUnit.Framework;
using ServiceStack;
using ServiceStack.Aws.S3;
using ServiceStack.Testing;

namespace AwsStacks
{
    [TestFixture]
    public class AdminTasks
    {
        private readonly ServiceStackHost appHost;
        S3VirtualPathProvider s3;

        public AdminTasks()
        {
            appHost = new BasicAppHost().Init();
            s3 = AwsConfig.CreateS3VirtualPathProvider(appHost, AwsConfig.S3BucketName);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Drop_and_Create_ssawsdemo_Bucket()
        {
            try
            {
                s3.ClearBucket();
                s3.AmazonS3.DeleteBucket(AwsConfig.S3BucketName);
            }
            catch { }

            s3.AmazonS3.PutBucket(AwsConfig.S3BucketName);
        }
        
    }
}