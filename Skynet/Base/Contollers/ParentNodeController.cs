using Newtonsoft.Json;
using Skynet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;

namespace Skynet.Base.Contollers
{
    class ParentNodeController : ApiController
    {
        //[Route("api/node/{nodeId}/parent")]
        //[HttpGet]
        public NodeResponse Get(string nodeId) {
            if (!Utils.Utils.isValidGuid(nodeId))
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.InvalidRequest,
                    description = "your node id is invalid",
                };
            }
            Node targetNode = Node.AllLocalNodes.Where(x => x.uuid == nodeId).DefaultIfEmpty(null).FirstOrDefault();
            if (targetNode == null)
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.NotFound,
                    description = "target node cannot be found on the client",
                };
            }
            return new NodeResponse
            {
                statusCode = NodeResponseCode.OK,
                description = "get target node info success",
                value = JsonConvert.SerializeObject(targetNode.getInfo())
            };
        }
    }
}
