using Newtonsoft.Json;
using SharpTox.Core;
using Skynet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Skynet.Base.Contollers
{
    class NodeController
    {
        public static Task<Response> DataControl(Request req) {
            return Task.Factory.StartNew(() =>
            {
                if (new Regex(@"^node/?$").IsMatch(req.url))
                {
                    return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                    {
                        statusCode = NodeResponseCode.InvalidRequest,
                        description = "Your request invalid, node id is missing in you request url",
                    }));
                }

                if (!new Regex(@"^node/[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}/?").IsMatch(req.url))
                {
                    return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                    {
                        statusCode = NodeResponseCode.InvalidRequest,
                        description = "your node id is invalid",
                    }));
                }

                string targetNodeId = req.url.Split('/')[1];
                Node targetNode = Node.AllLocalNodes.Where(x => x.uuid == targetNodeId).DefaultIfEmpty(null).FirstOrDefault();
                if (targetNode == null)
                {
                    return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                    {
                        statusCode = NodeResponseCode.NotFound,
                        description = "target node cannot be found on the client",
                    }));
                }

                if (new Regex(@"^node/[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}/?$").IsMatch(req.url))
                {
                    // get taget node info
                    if (req.method.ToLower() == "get")
                    {
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.OK,
                            description = "get target node info success",
                            value = JsonConvert.SerializeObject(targetNode.getInfo())
                        }));
                    }

                    // other method are not supported
                    return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                    {
                        statusCode = NodeResponseCode.InvalidRequestMethod,
                        description = "Your request method is not allowed"
                    }));
                }

                if (new Regex(@"^node/[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}/parent/?$").IsMatch(req.url))
                {
                    if (req.method.ToLower() == "get")
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.OK,
                            description = "get node parent info success",
                            value = JsonConvert.SerializeObject(targetNode.parent),
                        }));

                    else if (req.method.ToLower() == "put" || req.method.ToLower() == "merge")
                    {
                        // check lock
                        if (targetNode.nodeChangeLock.isLocked == true)
                        {
                            // target is locked, cannot be changed at this time
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.TargetLocked,
                                description = "target is locked",
                            }));
                        }
                        else
                        {
                            NodeId newParent = JsonConvert.DeserializeObject<NodeId>(req.content);
                            targetNode.parent = newParent;
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.OK,
                                description = "set parent success",
                            }));
                        }
                    }

                    else
                    {
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.InvalidRequestMethod,
                            description = "Your request method is invalid or not allowed",
                        }));
                    }
                }

                if (new Regex(@"^node/[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}/childNodes/?$").IsMatch(req.url))
                {
                    if (req.method.ToLower() == "get")
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.OK,
                            description = "get node child info success",
                            value = JsonConvert.SerializeObject(targetNode.childNodes),
                        }));

                    else if (req.method.ToLower() == "put" || req.method.ToLower() == "merge")
                    {
                        // check lock
                        if (targetNode.nodeChangeLock.isLocked == true)
                        {
                            // target is locked, cannot be changed at this time
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.TargetLocked,
                                description = "target is locked",
                            }));
                        }
                        else
                        {
                            List<NodeId> newChildNodes = JsonConvert.DeserializeObject<List<NodeId>>(req.content);
                            targetNode.childNodes = newChildNodes;
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.OK,
                                description = "set child nodes success",
                            }));
                        }
                    }

                    else if (req.method.ToLower() == "post")
                    {
                        if (targetNode.nodeChangeLock.isLocked)
                        {
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.TargetLocked,
                                description = "target is locked",
                            }));
                        }
                        else if (targetNode.childNodes.Count >= Node.MAX_CHILD_NODES_NUM)
                        {
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.TargetIsFull,
                                description = "target children is full, and can not add any more",
                                value = JsonConvert.SerializeObject(targetNode.childNodes), // return child nodes as alternative options 
                            }));
                        }
                        else
                        {
                            NodeId newChildNode = JsonConvert.DeserializeObject<NodeId>(req.content);
                            if (!targetNode.childNodes.Contains<NodeId>(newChildNode))
                            {
                                targetNode.childNodes.Add(newChildNode);
                            }
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.OK,
                                description = "add child node success",
                            }));
                        }
                    }

                    else
                    {
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.InvalidRequestMethod,
                            description = "Your request method is invalid or not allowed",
                        }));
                    }
                }

                if (new Regex(@"^node/[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}/childNodes/[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}?/$")
                    .IsMatch(req.url))
                {
                    string targetNodeUuid = req.url.Split('/')[3];
                    NodeId targetChildNode = targetNode.childNodes.Where(x => x.uuid == targetNodeUuid).DefaultIfEmpty(null).FirstOrDefault();
                    if (targetChildNode == null)
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.NotFound,
                            description = "target node cannot be found on the client",
                        }));
                    if (req.method.ToLower() == "delete")
                    {
                        if (targetNode.nodeChangeLock.isLocked)
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.TargetLocked,
                                description = "target is locked",
                            }));
                        targetNode.childNodes.Remove(targetChildNode);
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.OK,
                            description = "delete target child node success",
                        }));
                    }

                    else if (req.method.ToLower() == "get")
                    {
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.OK,
                            description = "get target node info success",
                            value = JsonConvert.SerializeObject(targetChildNode),
                        }));
                    }
                    else
                    {
                        // other methods are not allowed
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.InvalidRequestMethod,
                            description = "Your request method is invalid or not allowed"
                        }));
                    }
                }

                // brother nodes related
                if (new Regex(@"^node/[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}/brotherNodes/?$").IsMatch(req.url))
                {
                    if (req.method.ToLower() == "get")
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.OK,
                            description = "get node parent info success",
                            value = JsonConvert.SerializeObject(targetNode.brotherNodes),
                        }));

                    else if (req.method.ToLower() == "put" || req.method.ToLower() == "merge")
                    {
                        // check lock
                        if (targetNode.nodeChangeLock.isLocked == true)
                        {
                            // target is locked, cannot be changed at this time
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.TargetLocked,
                                description = "target is locked",
                            }));
                        }
                        else
                        {
                            List<NodeId> newBrotherNodes = JsonConvert.DeserializeObject<List<NodeId>>(req.content);
                            targetNode.brotherNodes = newBrotherNodes;
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.OK,
                                description = "set brother nodes success",
                            }));
                        }
                    }

                    else if (req.method.ToLower() == "post")
                    {
                        if (targetNode.nodeChangeLock.isLocked)
                        {
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.TargetLocked,
                                description = "target is locked",
                            }));
                        }
                        else if (targetNode.brotherNodes.Count >= Node.MAX_CHILD_NODES_NUM)
                        {
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.TargetIsFull,
                                description = "target brother is full, and can not add any more",
                            }));
                        }
                        else
                        {
                            NodeId newBrotherNode = JsonConvert.DeserializeObject<NodeId>(req.content);
                            if (!targetNode.brotherNodes.Contains<NodeId>(newBrotherNode))
                            {
                                targetNode.brotherNodes.Add(newBrotherNode);
                            }
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.OK,
                                description = "add brother node success",
                            }));
                        }
                    }

                    if (new Regex(@"^node/[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}/grandParents/?$").IsMatch(req.url))
                    {
                        if (req.method.ToLower() == "get")
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.OK,
                                description = "get node grandparents info success",
                                value = JsonConvert.SerializeObject(targetNode.grandParents),
                            }));

                        else if (req.method.ToLower() == "put" || req.method.ToLower() == "merge")
                        {
                            // check lock
                            if (targetNode.nodeChangeLock.isLocked == true)
                            {
                                // target is locked, cannot be changed at this time
                                return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                                {
                                    statusCode = NodeResponseCode.TargetLocked,
                                    description = "target is locked",
                                }));
                            }
                            else
                            {
                                NodeId newGrandParent = JsonConvert.DeserializeObject<NodeId>(req.content);
                                targetNode.grandParents = newGrandParent;
                                return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                                {
                                    statusCode = NodeResponseCode.OK,
                                    description = "set grandparents success",
                                }));
                            }
                        }

                        else
                        {
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.InvalidRequestMethod,
                                description = "Your request method is invalid or not allowed",
                            }));
                        }
                    }

                    else
                    {
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.InvalidRequestMethod,
                            description = "Your request method is invalid or not allowed",
                        }));
                    }
                }

                if (new Regex(@"^node/[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}/brotherNodes/[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}?/$")
                    .IsMatch(req.url))
                {
                    string targetNodeUuid = req.url.Split('/')[3];
                    NodeId targetChildNode = targetNode.brotherNodes.Where(x => x.uuid == targetNodeUuid).DefaultIfEmpty(null).FirstOrDefault();
                    if (targetChildNode == null)
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.NotFound,
                            description = "target node cannot be found on the client",
                        }));
                    if (req.method.ToLower() == "delete")
                    {
                        if (targetNode.nodeChangeLock.isLocked)
                            return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                            {
                                statusCode = NodeResponseCode.TargetLocked,
                                description = "target is locked",
                            }));
                        targetNode.brotherNodes.Remove(targetChildNode);
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.OK,
                            description = "delete target child node success",
                        }));
                    }

                    else if (req.method.ToLower() == "get")
                    {
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.OK,
                            description = "get target node info success",
                            value = JsonConvert.SerializeObject(targetChildNode),
                        }));
                    }
                    else
                    {
                        // other methods are not allowed
                        return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                        {
                            statusCode = NodeResponseCode.InvalidRequestMethod,
                            description = "Your request method is invalid or not allowed"
                        }));
                    }
                }

                return req.createResponse(JsonConvert.SerializeObject(new NodeResponse
                {
                    statusCode = NodeResponseCode.NotFound,
                    description = "target resource cannot be found on this client",
                }));
            });
        }
        
    }

    

    public class NodeResponse {
        public NodeResponseCode statusCode;
        public string description;
        public string value;
    }

    public enum NodeResponseCode {
        NotFound,
        OK,
        InvalidRequest,
        InvalidRequestMethod,
        TargetLocked,
        TargetIsFull
    }
}
