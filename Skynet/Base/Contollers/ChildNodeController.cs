using Newtonsoft.Json;
using Skynet.Models;
using System.Linq;
using System.Web.Http;

namespace Skynet.Base.Contollers
{
    public class ChildNodeController : ApiController
    {
        [Route("api/node/{nodeId}/childNodes")]
        [HttpGet]
        public NodeResponse GetAll(string nodeId) {
            if (!Utils.Utils.isValidGuid(nodeId))
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.InvalidRequest,
                    description = "your node id is invalid",
                };
            }
            Node targetNode = Node.AllLocalNodes.Where(x => x.selfNode.uuid == nodeId).DefaultIfEmpty(null).FirstOrDefault();
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
                description = "success",
                value = JsonConvert.SerializeObject(targetNode.childNodes)
            };
        }

        [Route("api/node/{nodeId}/childNodes/{childNodeId}")]
        [HttpGet]
        public NodeResponse Get(string nodeId, string childNodeId)
        {
            if (!Utils.Utils.isValidGuid(nodeId))
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.InvalidRequest,
                    description = "your node id is invalid",
                };
            }
            Node targetNode = Node.AllLocalNodes.Where(x => x.selfNode.uuid == nodeId).DefaultIfEmpty(null).FirstOrDefault();
            if (targetNode == null)
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.NotFound,
                    description = "target node cannot be found on the client",
                };
            }

            if (!Utils.Utils.isValidGuid(childNodeId))
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.InvalidRequest,
                    description = "your child node id is invalid",
                };
            }

            NodeId childNode = targetNode.childNodes.Where(x => x.uuid == childNodeId).DefaultIfEmpty(null).FirstOrDefault();
            if (childNode == null)
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.NotFound,
                    description = "target child node cannot be found on the client",
                };
            }

            return new NodeResponse
            {
                statusCode = NodeResponseCode.OK,
                description = "success",
                value = JsonConvert.SerializeObject(childNode),
            };
        }

        [Route("api/node/{nodeId}/childNodes")]
        [HttpPost]
        public NodeResponse Post(string nodeId, [FromBody]NodeId newNode) {
            if (!Utils.Utils.isValidGuid(nodeId))
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.InvalidRequest,
                    description = "your node id is invalid",
                };
            }
            Node targetNode = Node.AllLocalNodes.Where(x => x.selfNode.uuid == nodeId).DefaultIfEmpty(null).FirstOrDefault();
            if (targetNode == null)
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.NotFound,
                    description = "target node cannot be found on the client",
                };
            }

            // checklock
            if (targetNode.nodeChangeLock.isLocked)
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.TargetLocked,
                    description = "target node is currently locked",
                    value = JsonConvert.SerializeObject(targetNode.childNodes),
                };
            if (targetNode.childNodes.Count >= 10)
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.TargetIsFull,
                    description = "target node childnode is full",
                    value = JsonConvert.SerializeObject(targetNode.childNodes),
                };
            // check if already a child node
            if (targetNode.childNodes.Where(x => x.uuid == newNode.uuid).Count() != 0)
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.AlreadyExist,
                    description = "target is already a child node"
                };

            // add new child node
            targetNode.childNodes.Add(newNode);
            return new NodeResponse {
                statusCode = NodeResponseCode.OK,
                description = "add child node success",
                value = JsonConvert.SerializeObject(newNode),
            };
        }

        [Route("api/node/{nodeId}/childNodes/{childNodeId}")]
        [HttpDelete]
        public NodeResponse Delete(string nodeId, string childNodeId) {
            if (!Utils.Utils.isValidGuid(nodeId))
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.InvalidRequest,
                    description = "your node id is invalid",
                };
            }
            Node targetNode = Node.AllLocalNodes.Where(x => x.selfNode.uuid == nodeId).DefaultIfEmpty(null).FirstOrDefault();
            if (targetNode == null)
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.NotFound,
                    description = "target node cannot be found on the client",
                };
            }

            if (!Utils.Utils.isValidGuid(childNodeId))
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.InvalidRequest,
                    description = "your child node id is invalid",
                };
            }

            NodeId childNode = targetNode.childNodes.Where(x => x.uuid == childNodeId).DefaultIfEmpty(null).FirstOrDefault();
            if (childNode == null)
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.NotFound,
                    description = "target child node cannot be found on the client",
                };
            }
            targetNode.childNodes.Remove(childNode);
            return new NodeResponse {
                statusCode = NodeResponseCode.OK,
                description = "child node has been removed."
            };
        }

        [Route("api/node/{nodeId}/childNodes/{childNodeId}")]
        [HttpPut]
        public NodeResponse Put(string nodeId, string childNodeId, [FromBody] NodeId newNode) {
            if (!Utils.Utils.isValidGuid(nodeId))
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.InvalidRequest,
                    description = "your node id is invalid",
                };
            }
            Node targetNode = Node.AllLocalNodes.Where(x => x.selfNode.uuid == nodeId).DefaultIfEmpty(null).FirstOrDefault();
            if (targetNode == null)
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.NotFound,
                    description = "target node cannot be found on the client",
                };
            }

            if (!Utils.Utils.isValidGuid(childNodeId))
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.InvalidRequest,
                    description = "your child node id is invalid",
                };
            }

            NodeId childNode = targetNode.childNodes.Where(x => x.uuid == childNodeId).DefaultIfEmpty(null).FirstOrDefault();
            if (childNode == null)
            {
                return new NodeResponse
                {
                    statusCode = NodeResponseCode.NotFound,
                    description = "target child node cannot be found on the client",
                };
            }

            targetNode.childNodes.Remove(childNode);
            targetNode.childNodes.Add(newNode);
            return new NodeResponse {
                statusCode = NodeResponseCode.OK,
                description = "success changed target childnode",
            };
        }

        Task<>

    }
}
