using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Net.Http;
using Skynet.Models;
using System.Collections.Generic;
using Skynet.Base.Contollers;
using Newtonsoft.Json;

namespace SkynetTests.Base.Controllers
{
    [TestClass]
    public class ParentNodeControllerTest
    {

        Skynet.Base.Skynet mSkynet;
        string baseUrl;
        public ParentNodeControllerTest() {
            mSkynet = new Skynet.Base.Skynet();
            baseUrl = baseUrl = "http://localhost:" + mSkynet.httpPort + "/";
        }

        [TestMethod]
        public void Get()
        {
            Task.Run(async () =>
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(200); // This process may take up to 2mins
                    // create a node
                    Node mNode = new Node(new List<NodeId>(), mSkynet);
                    // set http headers
                    client.DefaultRequestHeaders.Add("Uuid", Guid.NewGuid().ToString());
                    client.DefaultRequestHeaders.Add("From-Node-Id", mNode.selfNode.uuid);
                    client.DefaultRequestHeaders.Add("From-Tox-Id", mNode.selfNode.toxid);
                    client.DefaultRequestHeaders.Add("To-Node-Id", mNode.selfNode.uuid);
                    client.DefaultRequestHeaders.Add("To-Tox-Id", mSkynet.tox.Id.ToString());
                    string res = await client.GetStringAsync(baseUrl + "api/tox/" + ""); // the tox id does not exist
                    NodeResponse mRes = JsonConvert.DeserializeObject<NodeResponse>(res);
                    Assert.AreEqual(mRes.statusCode, NodeResponseCode.NotFound);
                }
            });
        }
    }
}
