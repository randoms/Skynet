using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using SharpTox.Core;
using Skynet.Base.Contollers;
using Skynet.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Skynet.Base
{
    public class Skynet
    {

        public Tox tox;
        private Dictionary<string, Package> mPackageCache = new Dictionary<string, Package>();
        private Dictionary<string, Action<ToxResponse>> mPendingReqList = new Dictionary<string, Action<ToxResponse>>();
        public static int MAX_MSG_LENGTH = 1024;
        private List<string> connectedList = new List<string>();
        public int httpPort;
        

        public Skynet()
        {
            // init tox client
            ToxOptions options = new ToxOptions(true, true);
            
            tox = new Tox(options);
            tox.OnFriendRequestReceived += tox_OnFriendRequestReceived;
            tox.OnFriendMessageReceived += tox_OnFriendMessageReceived;
            tox.OnFriendConnectionStatusChanged += tox_OnFriendConnectionStatusChanged;

            foreach (ToxNode node in Nodes)
                tox.Bootstrap(node);

            tox.Name = "Skynet";
            tox.StatusMessage = "Running Skynet";
            tox.Start();

            string id = tox.Id.ToString();
            Console.WriteLine("ID: {0}", id);

            // start http server
            httpPort = Utils.Utils.FreeTcpPort();
            string baseUrl = "http://localhost:" + httpPort + "/";
            WebApp.Start<StartUp>(url: baseUrl);
        }

        static ToxNode[] Nodes = new ToxNode[]
        {
            new ToxNode("198.98.51.198", 33445, new ToxKey(ToxKeyType.Public, "1D5A5F2F5D6233058BF0259B09622FB40B482E4FA0931EB8FD3AB8E7BF7DAF6F"))
        };

        void tox_OnFriendMessageReceived(object sender, ToxEventArgs.FriendMessageEventArgs e)
        {
            //get the name associated with the friendnumber
            string name = tox.GetFriendName(e.FriendNumber);
            //print the message to the console
            Console.WriteLine("<{0}> {1}", name, e.Message);

            Package receivedPackage = JsonConvert.DeserializeObject<Package>(e.Message);
            
            if(receivedPackage.currentCount == 0)
            {
                if(receivedPackage.totalCount == 1)
                {
                    newReqReceived(receivedPackage, e.FriendNumber);
                    return;
                }
                mPackageCache.Add(receivedPackage.uuid, receivedPackage);
            }else if(receivedPackage.currentCount != receivedPackage.totalCount - 1)
            {
                mPackageCache[receivedPackage.uuid].content += receivedPackage.content;
            }else if(receivedPackage.currentCount == receivedPackage.totalCount - 1)
            {
                newReqReceived(receivedPackage, e.FriendNumber);
            }
        }

        void tox_OnFriendRequestReceived(object sender, ToxEventArgs.FriendRequestEventArgs e)
        {
            //automatically accept every friend request we receive
            tox.AddFriendNoRequest(e.PublicKey);
            Console.WriteLine("Received friend req: " + e.PublicKey);
        }

        void tox_OnFriendConnectionStatusChanged(object sender, ToxEventArgs.FriendConnectionStatusEventArgs e) {
            // find target friend in all nodes
            Node.AllLocalNodes.ForEach((mnode) => {
                List<NodeId> relatedNodes = mnode.childNodes.Concat(mnode.brotherNodes).ToList();
                relatedNodes.Add(mnode.parent);
                relatedNodes.Add(mnode.grandParents);
                relatedNodes.
                Where(x => new ToxId(x.toxid).PublicKey.GetString() == tox.GetFriendPublicKey(e.FriendNumber).GetString())
                .ToList().ForEach(nodeToRemove => {
                    mnode.relatedNodesStatusChanged(nodeToRemove);
                });
            });
        }

        bool sendResponse(ToxResponse res, ToxId toxid)
        {
            string resContent = JsonConvert.SerializeObject(res);
            int packageNum = resContent.Length / MAX_MSG_LENGTH + 1;
            bool result = false;
            for (int i = 0; i < packageNum; i++)
            {
                string mcontent = "";
                if (i * MAX_MSG_LENGTH + MAX_MSG_LENGTH > resContent.Length)
                    mcontent = resContent.Substring(i * MAX_MSG_LENGTH);
                else
                    mcontent = resContent.Substring(i * MAX_MSG_LENGTH, MAX_MSG_LENGTH);
                result = sendMsg(toxid, JsonConvert.SerializeObject(new Package
                {
                    uuid = res.uuid,
                    totalCount = packageNum,
                    currentCount = i,
                    content = mcontent,
                }));
            }
            return result;
        }

        bool sendResponse(ToxResponse res, ToxKey toxkey)
        {
            return sendResponse(res, new ToxId(toxkey.GetBytes(), 100));
        }

        void newReqReceived(Package receivedPackage, int friendNum)
        {
            string mcontentCache = "";
            if (mPackageCache.ContainsKey(receivedPackage.uuid))
                mcontentCache = mPackageCache[receivedPackage.uuid].content;
            mcontentCache += receivedPackage.content;
            // check if this is a response
            if (mPendingReqList.ContainsKey(receivedPackage.uuid))
            {
                mPendingReqList[receivedPackage.uuid](JsonConvert.DeserializeObject<ToxResponse>(mcontentCache));
                mPendingReqList.Remove(receivedPackage.uuid);
                return;
            }

            ToxRequest newReq = JsonConvert.DeserializeObject<ToxRequest>(mcontentCache);
            Task.Factory.StartNew(async () =>
            {
                // send response to http server
                ToxResponse mRes = await RequestProxy.sendRequest(newReq);
                sendResponse(mRes, tox.GetFriendPublicKey(friendNum));
            });
        }

        public bool sendMsg(ToxKey toxkey, string msg)
        {
            return sendMsg(new ToxId(toxkey.GetBytes(), 100), msg);
        }

        public bool sendMsg(ToxId toxid, string msg)
        {
            ToxKey toxkey = toxid.PublicKey;
            int friendNum = tox.GetFriendByPublicKey(toxkey);
            if (friendNum == -1)
            {
                int res = tox.AddFriend(toxid, "add friend");
                if (res != (int)ToxErrorFriendAdd.Ok)
                    return false;
                friendNum = tox.GetFriendByPublicKey(toxkey);
            }
            int waitCount = 0;
            int maxCount = 500;
            if (connectedList.IndexOf(toxkey.GetString()) == -1)
                maxCount = 1200; // first time wait for 120s
            while (tox.GetFriendConnectionStatus(friendNum) == ToxConnectionStatus.None && waitCount < maxCount)
            {
                if (waitCount % 100 == 0)
                    Console.WriteLine("target is offline." + waitCount/100);
                waitCount++;
                Thread.Sleep(10);
            }
            if (waitCount == maxCount)
            {
                Console.WriteLine("Connect Failed");
                connectedList.Remove(toxkey.GetString());
                return false;
            }
            connectedList.Add(toxkey.GetString());
            int msgRes = tox.SendMessage(friendNum, msg, ToxMessageType.Message);
            return msgRes > 0;
        }

        public Task<ToxResponse> sendRequest(ToxId toxid, ToxRequest req, out bool status) {
            string reqContent = JsonConvert.SerializeObject(req);
            int packageNum = reqContent.Length / MAX_MSG_LENGTH + 1;
            bool res = false;
            for (int i = 0; i < packageNum; i++)
            {
                string mcontent = "";
                if (i * MAX_MSG_LENGTH + MAX_MSG_LENGTH > reqContent.Length)
                    mcontent = reqContent.Substring(i * MAX_MSG_LENGTH);
                else
                    mcontent = reqContent.Substring(i * MAX_MSG_LENGTH, MAX_MSG_LENGTH);
                res = sendMsg(toxid, JsonConvert.SerializeObject(new Package
                {
                    uuid = req.uuid,
                    totalCount = packageNum,
                    currentCount = i,
                    content = mcontent,
                }));
                if (!res) {
                    status = false;
                    return Task.Factory.StartNew<ToxResponse>(()=> {
                        return null;
                    });
                }
            }
            status = res;
            bool isResponseReceived = false;
            ToxResponse mRes = null;
            if (res) {
                mPendingReqList.Add(req.uuid, (response)=> {
                    isResponseReceived = true;
                    mRes = response;
                });
            }
            return Task.Factory.StartNew(() =>
            {
                while (!isResponseReceived)
                {
                    Thread.Sleep(10);
                }
                return mRes;
            });
        }
    }
}
