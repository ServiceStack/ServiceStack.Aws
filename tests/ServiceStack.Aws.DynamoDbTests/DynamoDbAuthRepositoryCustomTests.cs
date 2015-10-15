using System;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Aws.DynamoDb;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Aws.DynamoDbTests
{
    public class CustomUserAuth : UserAuth
    {
        public string Custom { get; set; }
    }

    public class CustomUserAuthDetails : UserAuthDetails
    {
        public string Custom { get; set; }
    }

    [TestFixture]
    public class DynamoDbAuthRepositoryCustomTests : DynamoTestBase
    {
        private ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            DynamoMetadata.Reset();
            var db = CreatePocoDynamo();
            db.DeleteAllTables(TimeSpan.FromMinutes(1));

            appHost = new BasicAppHost()
                .Init();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        private IUserAuthRepository CreateAuthRepo(IPocoDynamo db)
        {
            var authRepo = new DynamoDbAuthRepository<CustomUserAuth, CustomUserAuthDetails>(db);
            authRepo.InitSchema();
            return authRepo;
        }

        [Test]
        public void Does_create_Custom_Auth_Tables()
        {
            var db = CreatePocoDynamo();
            var authRepo = CreateAuthRepo(db);
            authRepo.InitSchema();

            db.GetTableNames().PrintDump();

            Assert.That(db.GetTableNames(), Is.EquivalentTo(new[] {
                typeof(Seq).Name,
                typeof(CustomUserAuth).Name,
                typeof(CustomUserAuthDetails).Name,
                typeof(UserAuthRole).Name,
            }));

            var userAuth = AssertTable(db, typeof(CustomUserAuth), "Id");
            AssertIndex(userAuth.GlobalIndexes[0], "UsernameUserAuthIndex", "UserName", "Id");

            var userAuthDetails = AssertTable(db, typeof(CustomUserAuthDetails), "UserAuthId", "Id");
            AssertIndex(userAuthDetails.GlobalIndexes[0], "UserIdUserAuthDetailsIndex", "UserId", "Provider");

            var userAuthRole = AssertTable(db, typeof(UserAuthRole), "UserAuthId", "Id");
            AssertIndex(userAuthRole.LocalIndexes[0], "UserAuthRoleRoleIndex", "UserAuthId", "Role");
            AssertIndex(userAuthRole.LocalIndexes[1], "UserAuthRolePermissionIndex", "UserAuthId", "Permission");
        }

        [Test]
        public void Can_Create_CustomUserAuth()
        {
            var db = CreatePocoDynamo();
            var authRepo = CreateAuthRepo(db);
            authRepo.InitSchema();

            authRepo.CreateUserAuth(new CustomUserAuth
            {
                Custom = "CustomUserAuth",
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                Email = "demis.bellot@gmail.com",
            }, "test");

            authRepo.CreateUserAuth(new CustomUserAuth
            {
                Custom = "CustomUserAuth",
                DisplayName = "Credentials",
                FirstName = "First",
                LastName = "Last",
                FullName = "First Last",
                UserName = "mythz",
            }, "test");
        }
    }
}