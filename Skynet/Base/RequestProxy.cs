using Newtonsoft.Json;
using SharpTox.Core;
using Skynet.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Skynet.Base
{
    /// <summary>
    /// switch between sky req and http req
    /// </summary>
    class RequestProxy
    {
        public static async Task<Response> sendRequest(Request req)
        {
            // if req is not send to local node
            if (req.fromToxId != Skynet.tox.Id.ToString()) {
                // send req to remove tox client
                bool mResStatus = false;
                return await Skynet.getInstance().sendRequest(new ToxId(req.toToxId), req, out mResStatus);
            }
            // request was sent to local host
            using (var client = new HttpClient())
            {
                string baseUrl = "http://localhost:" + ConfigurationManager.AppSettings["port"] + "/";
                client.DefaultRequestHeaders.Add("Uuid", req.uuid);
                client.DefaultRequestHeaders.Add("From-Node-Id", req.fromNodeId);
                client.DefaultRequestHeaders.Add("From-Tox-Id", req.fromToxId);
                client.DefaultRequestHeaders.Add("To-Node-Id", req.toNodeId);
                client.DefaultRequestHeaders.Add("To-Tox-Id", req.toToxId);
                string responseString = await client.GetStringAsync(baseUrl + "api/" + req.url);
                return req.createResponse(responseString);
            }
        }

        
    }
}
