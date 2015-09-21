using Newtonsoft.Json;
using SharpTox.Core;
using Skynet.Base;
using Skynet.Base.Contollers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skynet.Models
{
    public class Node
    {
        public static List<Node> AllLocalNodes = new List<Node>();
        public Base.Skynet mSkynet { get; set; }

        public NodeId parent { get; set; }
        public NodeId grandParents { get; set; }
        public NodeId selfNode { get; set; }
        public List<NodeId> childNodes { get; set; }
        public List<NodeId> brotherNodes { get; set; }
        public NodeLock nodeChangeLock { get; set; }
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
            Task.Factory.StartNew(async () =>
            {
                await joinNetByTargetParents(bootStrapParents); // if successed parent will be set, and isConnected will set to true
            });
            nodeChangeLock = new NodeLock { from = null, isLocked = false };
            AllLocalNodes.Add(this);
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
        /// send msg to all the other nodes
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public Task boardCastAll(ToxRequest req)
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

            while (targetNodeList.Count > 0 && target == null)
            {
                NodeId parentNode = targetNodeList[0];
                ToxRequest addParentReq = new ToxRequest
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
                ToxResponse mRes = await RequestProxy.sendRequest(mSkynet, addParentReq);
                // send req failed or target is currently locked, ie target is not avaliable right now. remove target node from nodelist
                if (!addParentReqRes)
                {
                    targetNodeList.Remove(parentNode);
                    continue;
                }
                NodeResponse addParentResponse = JsonConvert.DeserializeObject<NodeResponse>(mRes.content);
                switch (addParentResponse.statusCode)
                {
                    case NodeResponseCode.TargetLocked:
                        targetNodeList.Remove(parentNode);
                        if (!checkedNodesList.Contains<NodeId>(parentNode))
                            checkedNodesList.Add(parentNode);
                        continue;
                    case NodeResponseCode.TargetIsFull:
                        targetNodeList.Remove(parentNode);
                        if (!checkedNodesList.Contains<NodeId>(parentNode))
                            checkedNodesList.Add(parentNode);
                        // new nodes, not checked yet
                        List<NodeId> newTargetsList = JsonConvert.DeserializeObject<List<NodeId>>(addParentResponse.value).Where((mnode) => {
                            return !checkedNodesList.Contains<NodeId>(mnode);
                        }).ToList();
                        targetNodeList.Concat(newTargetsList);
                        continue;
                    case NodeResponseCode.OK:
                        // set parent and connect status
                        target = new NodeId {
                            toxid = mRes.fromToxId,
                            uuid = mRes.fromNodeId
                        };
                        isConnected = true;
                        break;
                }

            }

            if (target != null)
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

        public void relatedNodesStatusChanged(NodeId targetNode) {
            if (targetNode == nodeChangeLock.from)
                nodeChangeLock.isLocked = false;
            // child nodes offline
            NodeId childNodeToRemove = childNodes.Where(x => x.uuid == targetNode.uuid).DefaultIfEmpty(null).FirstOrDefault();
            if (childNodeToRemove != null) {
                childNodes.Remove(targetNode);
                childNodes.ForEach(remindingNodes => {
                    bool status = false;
                    // send request to reminding nodes
                    mSkynet.sendRequest(new ToxId(remindingNodes.toxid), new ToxRequest
                    {
                        url = "node/" + remindingNodes.uuid + "/brotherNodes/" + childNodeToRemove.uuid,
                        method = "delete",
                        uuid = Guid.NewGuid().ToString(),
                        content = "",
                        fromNodeId = selfNode.uuid,
                        fromToxId = selfNode.toxid,
                        toNodeId = remindingNodes.toxid,
                        toToxId = remindingNodes.toxid
                    }, out status);
                });
                // remove nodes from friends
                
            }
            // parent node offline
            if(targetNode.uuid == parent.uuid)
            {
                Task.Run(async () =>
                {
                    await joinNetByTargetParents(brotherNodes);
                    // send new grand parents to child nodes
                    childNodes.ForEach(child =>
                    {
                        bool status = false;
                        // send request to reminding nodes
                        mSkynet.sendRequest(new ToxId(child.toxid), new ToxRequest
                        {
                            url = "node/" + child.uuid + "/grandParents",
                            method = "put",
                            uuid = Guid.NewGuid().ToString(),
                            content = JsonConvert.SerializeObject(parent),
                            fromNodeId = selfNode.uuid,
                            fromToxId = selfNode.toxid,
                            toNodeId = child.toxid,
                            toToxId = child.toxid
                        }, out status);
                    });
                });
            }
            // brother nodes offline just delete friend record
            if (brotherNodes.IndexOf(targetNode) != -1) {
                brotherNodes.Remove(targetNode);
            }
            // grandparent node offline just delete friend record
            ToxErrorFriendDelete mError = ToxErrorFriendDelete.Ok;
            int targetFriendNum = mSkynet.tox.GetFriendByPublicKey(new ToxId(childNodeToRemove.toxid).PublicKey);
            mSkynet.tox.DeleteFriend(targetFriendNum, out mError);
        }

        public NodeInfo getInfo() {
            return new NodeInfo
            {
                uuid = selfNode.uuid,
                parent = parent,
                grandParents = grandParents,
                selfNode = selfNode,
                childNodes = childNodes,
                brotherNodes = brotherNodes,
                nodeChangeLock = nodeChangeLock,
                startTime = startTime,
                diskFreeSpace = diskFreeSpace,
                bandWidth = bandWidth,
                MAX_CHILD_NODES_NUM = MAX_CHILD_NODES_NUM,
                isConnected = isConnected,
            };
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

        override
        public string ToString() {
            return uuid;
        }
    }

    public class NodeInfo {
        public string uuid { get; set; }
        public NodeId parent { get; set; }
        public NodeId grandParents { get; set; }
        public NodeId selfNode { get; set; }
        public List<NodeId> childNodes { get; set; }
        public List<NodeId> brotherNodes { get; set; }
        public NodeLock nodeChangeLock { get; set; }
        public DateTime startTime { get; set; }
        public int diskFreeSpace { get; set; } // unit MB
        public int bandWidth { get; set; } // unit KB
        public int MAX_CHILD_NODES_NUM = 10;
        public bool isConnected = false; // is the node is connected to its net
    }

    public class NodeLock {
        public bool isLocked;
        public NodeId from;
    }
}
