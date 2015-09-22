using System;
using System.Threading.Tasks;
using Skynet.Models;
using System.Collections.Generic;
using SharpTox.Core;
using Skynet.Base.Contollers;
using Newtonsoft.Json;
using System.Threading;
using System.Text;
using System.Net;
using System.IO;
using Skynet.Base;

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
            List<NodeId> parents = new List<NodeId>();
            Node testNode = new Node(parents, mSkynet);
            Console.ReadKey();
        }
    }
}