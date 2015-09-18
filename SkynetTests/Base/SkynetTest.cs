using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpTox.Core;
using Skynet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkynetTests.Base
{
    [TestClass]
    public class SkynetTest
    {
        Skynet.Base.Skynet mSkynet;

        public SkynetTest() {
            mSkynet = new Skynet.Base.Skynet();
        }

        [TestMethod]
        public void SendRequest() {
            bool status = false;
            Task.Run(async () => {
                Node mNode = new Node(new List<NodeId>(), mSkynet);
                var res = await mSkynet.sendRequest(new ToxId("062AA695A9F3E8C6A6667E5BCD24B16ABF96F4775EE3E59764FDFC2453C4027A74FA2E4D26BC"), new ToxRequest {
                    url = "",
                    uuid = Guid.NewGuid().ToString(),
                    content = "test",
                    fromNodeId = mNode.selfNode.uuid,
                    fromToxId = Skynet.Base.Skynet.webServiceHost.tox.Id.ToString(),
                    toNodeId = mNode.selfNode.uuid,
                    toToxId = "062AA695A9F3E8C6A6667E5BCD24B16ABF96F4775EE3E59764FDFC2453C4027A74FA2E4D26BC",
                }, out status);
                Assert.AreEqual(status, false);
            }).GetAwaiter().GetResult();
        }
    }
}
