using System;
using Amazon;
using NUnit.Framework;
using ServiceStack.Aws.S3;
using ServiceStack.Aws.Tests.Services;

namespace ServiceStack.Aws.Tests.S3
{
    [TestFixture, Category("Integration")]
    [Explicit]
    public class S3FileStorageProviderTests : FileStorageProviderCommonTests
    {
        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            // In order to test with a real S3 instance, enter the appropriate auth information below and run these tests explicitly
            var s3ConnectionFactory = new S3ConnectionFactory("accessKeyId", "secretAccessKey", RegionEndpoint.USEast1);
            
            _providerFactory = () => new S3FileStorageProvider(s3ConnectionFactory);

            _baseFolderName = TestSubDirectory;

            Initialize();
        }
    }
}
