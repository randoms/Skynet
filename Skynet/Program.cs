using System;
using SharpTox.Core;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Owin.Hosting;
using Skynet.Base;
using System.Configuration;

namespace Skynet
{
    class Program
    {

        static void Main(string[] args)
        {
            Base.Skynet mSkynet = new Base.Skynet();
            string baseUrl = "http://localhost:" + ConfigurationManager.AppSettings["port"] + "/";
            Console.ReadLine();
        }
    }
}
