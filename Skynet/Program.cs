using System;
using SharpTox.Core;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Owin.Hosting;
using Skynet.Base;
using System.Configuration;
using Skynet.Models;
using System.Collections.Generic;

namespace Skynet
{
    class Program
    {

        static void Main(string[] args)
        {
            Base.Skynet mSkynet = new Base.Skynet();

            bool status = false;
            Task.Run(async () => {
                Node mNode = new Node(new List<NodeId>(), mSkynet);
                var res = await mSkynet.sendRequest(new ToxId("062AA695A9F3E8C6A6667E5BCD24B16ABF96F4775EE3E59764FDFC2453C4027A74FA2E4D26BC"), new ToxRequest
                {
                    url = "",
                    uuid = Guid.NewGuid().ToString(),
                    content = "test",
                    fromNodeId = mNode.selfNode.uuid,
                    fromToxId = mSkynet.tox.Id.ToString(),
                    toNodeId = mNode.selfNode.uuid,
                    toToxId = "062AA695A9F3E8C6A6667E5BCD24B16ABF96F4775EE3E59764FDFC2453C4027A74FA2E4D26BC",
                }, out status);
            }).GetAwaiter().GetResult();

            Console.ReadLine();
        }
    }
}
