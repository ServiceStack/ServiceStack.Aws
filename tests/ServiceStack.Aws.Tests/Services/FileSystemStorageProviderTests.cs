using System.IO;
using NUnit.Framework;
using ServiceStack.Aws.Services;

namespace ServiceStack.Aws.Tests.Services
{
    [TestFixture]
    public class FileSystemStorageProviderTests : FileStorageProviderCommonTests
    {
        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            _providerFactory = () => FileSystemStorageProvider.Instance;
            _baseFolderName = Path.Combine(Path.GetTempPath(), TestSubDirectory);
            Initialize();
        }
    }
}
