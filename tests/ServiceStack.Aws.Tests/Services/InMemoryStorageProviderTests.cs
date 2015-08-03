using NUnit.Framework;
using ServiceStack.Aws.Services;

namespace ServiceStack.Aws.Tests.Services
{
    [TestFixture]
    public class InMemoryStorageProviderTests : FileStorageProviderCommonTests
    {
        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            _providerFactory = () => InMemoryFileStorageProvider.Instance;
            _baseFolderName = TestSubDirectory;
            Initialize();
        }
    }
}
