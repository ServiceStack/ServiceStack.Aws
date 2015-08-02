using NUnit.Framework;
using ServiceStack.Server.Tests.Shared;
using ServiceStack.Text;

namespace ServiceStack.Aws.Tests.PocoDynamoTests
{
    [TestFixture, Explicit]
    public class AddHocPocoDynamoTests
    {
        [Test]
        public void Can_get_Customer()
        {
            var db = PocoDynaboDbTests.CreateClient();
            PocoDynaboDbTests.CreateCustomerTables(db);

            var dbCustomer = db.GetById<Customer>(1);

            dbCustomer.PrintDump();
        }
    }
}