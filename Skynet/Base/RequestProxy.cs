using Newtonsoft.Json;
using SharpTox.Core;
using Skynet.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Skynet.Base
{
    /// <summary>
    /// switch between sky req and http req
    /// </summary>
    public class RequestProxy
    {
        // TODO: add all methods support
        /*public static async Task<ToxResponse> sendRequest(Skynet host, ToxRequest req)
        {
            // if req is not send to local node
            if (host.tox.Id.ToString() != req.toToxId) {
                // send req to remove tox client
                bool mResStatus = false;
                return await host.sendRequest(new ToxId(req.toToxId), req, out mResStatus);
            }
            // request was sent to local host
            using (var client = new HttpClient())
            {
                string baseUrl = "http://localhost:" + host.httpPort + "/";
                client.DefaultRequestHeaders.Add("Uuid", req.uuid);
                client.DefaultRequestHeaders.Add("From-Node-Id", req.fromNodeId);
                client.DefaultRequestHeaders.Add("From-Tox-Id", req.fromToxId);
                client.DefaultRequestHeaders.Add("To-Node-Id", req.toNodeId);
                client.DefaultRequestHeaders.Add("To-Tox-Id", req.toToxId);
                string responseString = await client.GetStringAsync(baseUrl + "api/" + req.url);
                return req.createResponse(responseString);
            }
        }*/

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

        public static async Task<ToxResponse> sendRequest(Skynet host, ToxRequest req) {

            // if req is not send to local node
            if (host.tox.Id.ToString() != req.toToxId) {
                bool mResStatus = false;
                return await host.sendRequest(new ToxId(req.toToxId), req, out mResStatus);
            }

            string baseUrl = "http://localhost:" + host.httpPort + "/";
            var request = (HttpWebRequest)WebRequest.Create(baseUrl + "api/" + req.url);
            request.Headers.Add("Uuid", req.uuid);
            request.Headers.Add("From-Node-Id", req.fromNodeId);
            request.Headers.Add("From-Tox-Id", req.fromToxId);
            request.Headers.Add("To-Node-Id", req.toNodeId);
            request.Headers.Add("To-Tox-Id", req.toToxId);
            request.Method = req.method.ToUpper();
            request.ContentType = "application/json";

            List<string> allowedMethods = new List<string> { "POST", "PUT", "PATCH" };
            if (allowedMethods.Any(x => x == req.method.ToUpper())) {
                // only the above methods are allowed to add body data
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(req.content);
                }
            }
            var response = await request.GetResponseAsync();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            return req.createResponse(responseString);
        }
    }
}
