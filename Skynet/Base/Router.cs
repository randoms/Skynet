using Skynet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skynet.Base
{
    class Router
    {
        
        private Dictionary<string, Func<Request, Task<Response>>> mMapping 
            = new Dictionary<string, Func<Request, Task<Response>>>();
        private Skynet net;

        public Router(Skynet net)
        {
            this.net = net;
        }

        public void Add(string url, Func<Request, Task<Response>> res)
        {
            mMapping.Add(url, res);
        }

        public async Task<Response> route(Request req)
        {
            return await mMapping[req.url](req);
        }
    }
}
