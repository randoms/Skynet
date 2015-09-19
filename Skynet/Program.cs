using System;
using System.Threading.Tasks;
using Skynet.Models;
using System.Collections.Generic;
using SharpTox.Core;
using Skynet.Base.Contollers;
using Newtonsoft.Json;
using System.Threading;

namespace Skynet
{
    class Program
    {

        static void Main(string[] args)
        {
            Base.Skynet mSkynet = new Base.Skynet();
            while (!mSkynet.tox.IsConnected) {
                Thread.Sleep(10);
            }
            Base.Skynet mSkynet2 = new Base.Skynet();
            Node mNode1 = new Node(new List<NodeId>(), mSkynet);
            Node mNode2 = new Node(new List<NodeId>(), mSkynet2);

            bool status = false;
            Task.Run(async () =>
            {

                ToxResponse res = await mSkynet2.sendRequest(mSkynet.tox.Id, new ToxRequest
                {
                    url = "tox/" + mSkynet.tox.Id.ToString(),
                    uuid = Guid.NewGuid().ToString(),
                    method = "get",
                    content = "test",
                    fromNodeId = mNode2.selfNode.uuid,
                    fromToxId = mNode2.selfNode.toxid,
                    toNodeId = mNode1.selfNode.uuid,
                    toToxId = mNode1.selfNode.toxid,
                }, out status);

                Console.WriteLine("status " + status);
                if (status)
                {
                    NodeResponse nodeRes = JsonConvert.DeserializeObject<NodeResponse>(res.content);
                    Console.WriteLine("value: " + nodeRes.value);
                }
                else {
                    Console.WriteLine("send req failed");
                }

            }).GetAwaiter().GetResult();
            Console.ReadLine();
        }
    }
}