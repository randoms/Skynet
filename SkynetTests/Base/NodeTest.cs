﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skynet.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SkynetTests.Base
{
    [TestClass]
    public class NodeTest
    {
        Skynet.Base.Skynet mSkynet;
        Skynet.Base.Skynet mSkynet2;
        Node node1;
        Node node2;
        Node node3;

        public NodeTest() {
            mSkynet = new Skynet.Base.Skynet();
            mSkynet2 = new Skynet.Base.Skynet();
        }

        [TestMethod]
        public void LockTest()
        {
            // check if we can change a node after lock is set
            Task.Run(() => {
                node1.nodeChangeLock.isLocked = true;
                node1.nodeChangeLock.from = node2.selfNode;

            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void JoinNetTest() {
            node1 = new Node(mSkynet);
            node2 = new Node(mSkynet);
            node3 = new Node(mSkynet2);

            Task.Run(async () => {
                bool isConnected2 = await node2.joinNetByTargetParents(new List<NodeId> { node1.selfNode});
                bool isConnected3 = await node3.joinNetByTargetParents(new List<NodeId> { node1.selfNode });
                Assert.AreEqual(true, isConnected2);
                Assert.AreEqual(node2.parent.uuid, node1.selfNode.uuid);
                Assert.AreEqual(true, isConnected3);
                Assert.AreEqual(node3.parent.uuid, node1.selfNode.uuid);
                Assert.AreEqual(true, node1.childNodes.Any(x => x.uuid == node2.selfNode.uuid));
                Assert.AreEqual(true, node1.childNodes.Any(x => x.uuid == node3.selfNode.uuid));
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void JoinNetLocked() {
            node1 = new Node(mSkynet);
            node2 = new Node(mSkynet);
            node3 = new Node(mSkynet);
            Task.Run(async () => {
                bool isConnected = await node2.joinNetByTargetParents(new List<NodeId> { node1.selfNode });
                Assert.AreEqual(true, isConnected);
                Assert.AreEqual(true, node1.childNodes.Any(x => x.uuid == node2.selfNode.uuid));
                Assert.AreEqual(node1.selfNode.uuid, node2.parent.uuid);
                // lock node1
                node1.nodeChangeLock.isLocked = true;
                node1.nodeChangeLock.from = node2.selfNode;
                // node3 add to network
                bool isConnected3 = await node3.joinNetByTargetParents(new List<NodeId> { node1.selfNode });
                Assert.AreEqual(true, isConnected3);
                // node3's parent shoud be node2, since node1 is locked
                Assert.AreEqual(node2.selfNode.uuid, node3.parent.uuid);
                Assert.AreEqual(true, node2.childNodes.Any(x => x.uuid == node3.selfNode.uuid));
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void JoinNetFull() {
            node1 = new Node(mSkynet);
            node2 = new Node(mSkynet);
            node3 = new Node(mSkynet);

            Task.Run(async () => {
                bool isConnected = await node2.joinNetByTargetParents(new List<NodeId> { node1.selfNode });
                Assert.AreEqual(true, isConnected);
                Assert.AreEqual(true, node1.childNodes.Any(x => x.uuid == node2.selfNode.uuid));
                Assert.AreEqual(node1.selfNode.uuid, node2.parent.uuid);
                // add some nodes to fill node1's childnodes
                for (int i = 0; i < 9; i++) {
                    node1.childNodes.Add(new NodeId {uuid = Guid.NewGuid().ToString(), toxid = mSkynet.tox.Id.ToString() });
                }
                // node3 add to network
                bool isConnected3 = await node3.joinNetByTargetParents(new List<NodeId> { node1.selfNode });
                Assert.AreEqual(true, isConnected3);
                // node3's parent shoud be node2, since node1 is full
                Assert.AreEqual(node2.selfNode.uuid, node3.parent.uuid);
                Assert.AreEqual(true, node2.childNodes.Any(x => x.uuid == node3.selfNode.uuid));
            }).GetAwaiter().GetResult();
        }

        [TestMethod]
        public void StateChangeTest() {

        }
    }
}
