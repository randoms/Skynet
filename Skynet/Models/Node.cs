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

        // modify times
        public long grandParentsModifiedTime = 0;
        public long parentModifiedTime = 0;
        public long childNodesModifiedTime = 0;
        public long brotherModifiedTime = 0;

        public Node(List<NodeId> bootStrapParents, Base.Skynet skynet)
        {
            mSkynet = skynet;
            startTime = new DateTime();
            selfNode = new NodeId
            {
                uuid = Guid.NewGuid().ToString(),
                toxid = skynet.tox.Id.ToString()
            };
            childNodes = new List<NodeId>();
            brotherNodes = new List<NodeId>();
            nodeChangeLock = new NodeLock { from = null, isLocked = false };
            AllLocalNodes.Add(this);
            if (bootStrapParents != null && bootStrapParents.Count > 0)
                Task.Run(async () => {
                    await joinNetByTargetParents(bootStrapParents);
                });
        }

        public Node(Base.Skynet skynet):this(new List<NodeId>() { }, skynet) {}

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
        public async Task<bool> joinNetByTargetParents(List<NodeId> parentsList)
        {
            List<NodeId> targetNodeList = parentsList;
            List<NodeId> checkedNodesList = new List<NodeId>();
            NodeId target = null;

            while (targetNodeList.Count > 0 && target == null)
            {
                NodeId parentNode = targetNodeList[0];
                ToxRequest addParentReq = new ToxRequest
                {
                    url = "node/"+ parentNode.uuid +"/childNodes",
                    method = "post",
                    uuid = Guid.NewGuid().ToString(),
                    content = JsonConvert.SerializeObject(selfNode),
                    fromNodeId = selfNode.uuid,
                    fromToxId = selfNode.toxid,
                    toNodeId = parentNode.uuid,
                    toToxId = parentNode.toxid,
                };
                ToxResponse mRes = await RequestProxy.sendRequest(mSkynet, addParentReq);
                // send req failed or target is currently locked, ie target is not avaliable right now. remove target node from nodelist
                NodeResponse addParentResponse = JsonConvert.DeserializeObject<NodeResponse>(mRes.content);

                if (addParentResponse.statusCode == NodeResponseCode.TargetLocked
                    || addParentResponse.statusCode == NodeResponseCode.TargetIsFull)
                {
                    targetNodeList.Remove(parentNode);
                    if (!checkedNodesList.Contains<NodeId>(parentNode))
                        checkedNodesList.Add(parentNode);
                    // new nodes, not checked yet
                    List<NodeId> newTargetsList = JsonConvert.DeserializeObject<List<NodeId>>(addParentResponse.value).Where((mnode) =>
                    {
                        return !checkedNodesList.Contains<NodeId>(mnode) && !targetNodeList.Contains<NodeId>(mnode);
                    }).ToList();
                    targetNodeList = targetNodeList.Concat(newTargetsList).ToList();
                    continue;
                }
                else if (addParentResponse.statusCode == NodeResponseCode.OK) {
                    // set parent and connect status
                    target = new NodeId
                    {
                        toxid = mRes.fromToxId,
                        uuid = mRes.fromNodeId
                    };
                    isConnected = true;
                    break;
                }else
                {
                    // try to connect next target
                    targetNodeList.Remove(parentNode);
                    if (!checkedNodesList.Contains<NodeId>(parentNode))
                        checkedNodesList.Add(parentNode);
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
            return isConnected;
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
                        method = "put",
                        uuid = Guid.NewGuid().ToString(),
                        content = "",
                        fromNodeId = selfNode.uuid,
                        fromToxId = selfNode.toxid,
                        toNodeId = remindingNodes.toxid,
                        toToxId = remindingNodes.toxid
                    }, out status);
                });
                // remove nodes from friends
                // a little diffcult
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
                grandParentsModifiedTime = grandParentsModifiedTime,
                parentModifiedTime = parentModifiedTime,
                childNodesModifiedTime = childNodesModifiedTime,
                brotherModifiedTime = brotherModifiedTime
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

        public long grandParentsModifiedTime { get; set; }
        public long parentModifiedTime { get; set; }
        public long childNodesModifiedTime { get; set; }
        public long brotherModifiedTime { get; set; }
    }

    public class NodeLock {
        public bool isLocked;
        public NodeId from;
    }
}
