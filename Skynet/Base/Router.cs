using Skynet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            Func<Request, Task<Response>> mRes = new Func<Request, Task<Response>>((Request mreq) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    return mreq.createResponse();
                });
            });
            mMapping.Keys.ToList().ForEach(key => {
                if (new Regex(key).IsMatch(req.url))
                    mRes = mMapping[key];
            });
            return await mRes(req);
        }
    }
}
