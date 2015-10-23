using Amazon.S3;
using NUnit.Framework;
using ServiceStack.Aws.S3;
using ServiceStack.IO;
using ServiceStack.Testing;
using ServiceStack.VirtualPath;

namespace ServiceStack.Aws.Tests.S3
{
    public class S3VirtualPathProviderTests : VirtualPathProviderTests
    {
        private IAmazonS3 client = AwsConfig.CreateAmazonS3Client();

        public override IVirtualPathProvider GetPathProvider()
        {
            return new S3VirtualPathProvider(client, "ss-ci-test", appHost);
        }
    }

    public class InMemoryVirtualPathProviderTests : VirtualPathProviderTests
    {
        public override IVirtualPathProvider GetPathProvider()
        {
            return new InMemoryVirtualPathProvider(appHost);
        }
    }

    [TestFixture]
    public abstract class VirtualPathProviderTests
    {
        public abstract IVirtualPathProvider GetPathProvider();

        protected ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BasicAppHost()
                .Init();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_create_file()
        {
            var pathProvider = GetPathProvider();

            var filePath = "dir/file.txt";
            pathProvider.AddFile(filePath, "file");

            var file = pathProvider.GetFile(filePath);

            Assert.That(file.ReadAllText(), Is.EqualTo("file"));
            Assert.That(file.Name, Is.EqualTo(filePath));
            Assert.That(file.Extension, Is.EqualTo("txt"));
        }
    }
}