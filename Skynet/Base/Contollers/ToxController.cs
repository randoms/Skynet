using Newtonsoft.Json;
using SharpTox.Core;
using Skynet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Skynet.Base.Contollers
{
    public class ToxController : ApiController 
    {
        [Route("api/tox/{id}")]
        [HttpGet]
        public async Task<NodeResponse> Get(string id)
        {
            if (!ToxId.IsValid(id))
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.InvalidRequest,
                    description = "your tox id is invalid",
                };
            }

            // check if target tox client is local client
            if (id == Skynet.tox.Id.ToString())
            {
                // list all nodes on target tox client
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.OK,
                    description = "success",
                    value = JsonConvert.SerializeObject(new ToxClient
                    {
                        Id = id,
                        nodes = Node.AllLocalNodes.Select(x => x.getInfo()).ToList()
                    })
                };
            }
            // if not send tox req to target tox client
            bool reqStatus = false;
            Response nodeResponse = await Skynet.getInstance().sendRequest(new ToxId(id), new Request {
                //fromNodeId = 
                // I need a node to send request
                // from info is missing
            }, out reqStatus);

            return new NodeResponse
            {

            };
        }

        [Route("test")]
        [HttpGet]
        public NodeResponse test() {
            return new NodeResponse { statusCode = NodeResponseCode.OK };
        }
    }

    
    
}
