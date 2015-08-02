using System.Collections.Generic;
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
            PocoDynaboDbTests.CreateTestTables(db);

            var dbCustomer = db.GetItemById<Customer>(1);

            dbCustomer.PrintDump();
        }

        [Test]
        public void Can_Put_Get_and_Delete_Deeply_Nested_Nodes()
        {
            var db = PocoDynaboDbTests.CreateClient();
            PocoDynaboDbTests.CreateTestTables(db);

            var nodes = new Node(1, "/root",
                new List<Node>
                {
                    new Node(2,"/root/2", new[] {
                        new Node(4, "/root/2/4", new [] {
                            new Node(5, "/root/2/4/5", new[] {
                                new Node(6, "/root/2/4/5/6"),
                            }),
                        }),
                    }),
                    new Node(3, "/root/3")
                });

            db.PutItem(nodes);

            var dbNodes = db.GetItemById<Node>(1);

            dbNodes.PrintDump();

            Assert.That(dbNodes, Is.EqualTo(nodes));
        }
    }
}