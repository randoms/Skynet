using Newtonsoft.Json;
using SharpTox.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skynet.Models
{
    class Node
    {
        public Base.Skynet mSkynet { get; set; }

        public string uuid { get; set; }
        public NodeId parent { get; set; }
        public NodeId grandParents { get; set; }
        public NodeId selfNode { get; set; }
        public List<NodeId> childNodes { get; set; }
        public List<NodeId> brotherNodes { get; set; }
        public bool nodeChangeLock { get; set; }
        public DateTime startTime { get; set; }
        public int diskFreeSpace { get; set; } // unit MB
        public int bandWidth { get; set; } // unit KB
        public static int MAX_CHILD_NODES_NUM = 10;
        public bool isConnected = false; // is the node is connected to its net

        public Node(List<NodeId> bootStrapParents, Base.Skynet skynet)
        {
            mSkynet = skynet;
            startTime = new DateTime();
            selfNode = new NodeId
            {
                uuid = Guid.NewGuid().ToString(),
                toxid = skynet.tox.Id.ToString()
            };
            Task.Factory.StartNew(async() =>
            {
                await joinNetByTargetParents(bootStrapParents); // if successed parent will be set, and isConnected will set to true
            });
        }

        /// <summary>
        /// get the quality of the node based on bandwidth, uptime, disk storage size etc
        /// </summary>
        /// <returns>quality of the node</returns>
        public int getQuality()
        {
            long uptime = (new DateTime() - startTime).Milliseconds;

            return 0;
        }

        /// <summary>
        /// change position with target node
        /// </summary>
        public void changePostion()
        {

        }

        /// <summary>
        /// set new parent node when parent if offline
        /// </summary>
        public void setNewParents()
        {

        }

        /// <summary>
        /// send msg to all the other nodes
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public Task boardCastAll(Request req)
        {
            return Task.Factory.StartNew(() =>
            {
                // boardcast to all nodes
            });
        }

        /// <summary>
        /// method to join skynet by set target parents
        /// </summary>
        /// <returns>
        /// the target distributed
        /// </returns>
        public async Task joinNetByTargetParents(List<NodeId> parentsList)
        {
            List<NodeId> targetNodeList = parentsList;
            List<NodeId> checkedNodesList = new List<NodeId>();
            NodeId target = null;

            while(targetNodeList.Count > 0 && target == null)
            {
                NodeId parentNode = targetNodeList[0];
                Request addParentReq = new Request
                {
                    url = "node/childNodes",
                    method = "post",
                    uuid = Guid.NewGuid().ToString(),
                    content = JsonConvert.SerializeObject(selfNode),
                    fromNodeId = selfNode.uuid,
                    fromToxId = selfNode.toxid,
                    toNodeId = parentNode.uuid,
                    toToxId = parentNode.toxid,
                };
                bool addParentReqRes = false;
                Response mRes = await mSkynet.sendRequest(new ToxId(parentNode.toxid), addParentReq, out addParentReqRes);
                // send req failed or target is currently locked, ie target is not avaliable right now. remove target node from nodelist
                if (!addParentReqRes)
                {
                    targetNodeList.Remove(parentNode);
                    continue;
                }
                AddParentResponse addParentResponse = JsonConvert.DeserializeObject<AddParentResponse>(mRes.content);
                switch (addParentResponse.status)
                {
                    case JoinSkynetResponseStatus.Locked:
                        targetNodeList.Remove(parentNode);
                        if(!checkedNodesList.Contains<NodeId>(parentNode))
                            checkedNodesList.Add(parentNode);
                        continue;
                    case JoinSkynetResponseStatus.Redirected:
                        targetNodeList.Remove(parentNode);
                        if (!checkedNodesList.Contains<NodeId>(parentNode))
                            checkedNodesList.Add(parentNode);
                        // new nodes, not checked yet
                        List<NodeId> newTargetsList = addParentResponse.parents.Where((mnode)=> {
                            return !checkedNodesList.Contains<NodeId>(mnode);
                        }).ToList();
                        targetNodeList.Concat(newTargetsList);
                        continue;
                    case JoinSkynetResponseStatus.OK:
                        // set parent and connect status
                        target = addParentResponse.parents[0];
                        isConnected = true;
                        break;
                }

            }

            if(target != null)
            {
                isConnected = true;
                parent = target;
            }
            else
            {
                parent = null;
                isConnected = false;
            }
        }

        /// <summary>
        /// other nodes want to 
        /// </summary>
        /// <returns>
        /// other node want to join net by setting this node as parent
        /// </returns>
        public Response onAddParentReq(NodeId node, Request req)
        {
            Response mRes = req.createResponse();
            if (this.nodeChangeLock)
            {
                // current node is locked, not allowed to change right now
                mRes.content = JsonConvert.SerializeObject(new AddParentResponse
                {
                    status = JoinSkynetResponseStatus.Locked
                });
                return mRes;
            }
            if(childNodes.Count >= MAX_CHILD_NODES_NUM)
            {
                // currnet child list is full, redirect req to child nodes
                mRes.content = JsonConvert.SerializeObject(new AddParentResponse
                {
                    status = JoinSkynetResponseStatus.Redirected,
                    parents = childNodes
                });
                return mRes;
            }
            childNodes.Add(JsonConvert.DeserializeObject<NodeId>(req.content));
            mRes.content = JsonConvert.SerializeObject(new AddParentResponse
            {
                status = JoinSkynetResponseStatus.OK,
                parents = new List<NodeId> {selfNode}
            });
            return mRes;
        }
    }

    public class NodeId: IEquatable<NodeId>
    {
        public string uuid { get; set; }
        public string toxid { get; set; }

        public bool Equals(NodeId other)
        {
            return uuid == other.uuid && toxid == other.toxid;       
        }
    }

    enum JoinSkynetResponseStatus
    {
        Locked,
        Redirected,
        OK
    }

    class AddParentResponse
    {
        public JoinSkynetResponseStatus status { get; set; }
        public List<NodeId> parents { get; set; }
    }
}
