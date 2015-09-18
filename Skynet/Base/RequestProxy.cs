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
        public static async Task<ToxResponse> sendRequest(ToxRequest req)
        {
            // if req is not send to local node
            if (!Skynet.LocalSkynet.Any(x => x.tox.Id.ToString() == req.fromToxId)) {
                // send req to remove tox client
                bool mResStatus = false;
                return await Skynet.webServiceHost.sendRequest(new ToxId(req.toToxId), req, out mResStatus);
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

        public static ToxRequest toNodeRequest(HttpRequestMessage req) {
            return new ToxRequest
            {
                url = req.RequestUri.ToString(),
                method = req.Method.ToString(),
                content = req.Content.ToString(),
                uuid = req.Headers.GetValues("Uuid").FirstOrDefault(),
                fromNodeId = req.Headers.GetValues("From-Node-Id").FirstOrDefault(),
                fromToxId = req.Headers.GetValues("From-Tox-Id").FirstOrDefault(),
                toNodeId = req.Headers.GetValues("To-Node-Id").FirstOrDefault(),
                toToxId = req.Headers.GetValues("To-Tox-Id").FirstOrDefault(),
            };
        }
    }
}
