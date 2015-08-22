using System;
using SharpTox.Core;
using System.Threading.Tasks;
using System.Threading;

namespace Skynet
{
    class Program
    {
        static Tox tox;

        static void Main(string[] args)
        {
            ToxOptions options = new ToxOptions(true, true);

            tox = new Tox(options);
            tox.OnFriendRequestReceived += tox_OnFriendRequestReceived;
            tox.OnFriendMessageReceived += tox_OnFriendMessageReceived;

            foreach (ToxNode node in Nodes)
                tox.Bootstrap(node);

            tox.Name = "SharpTox";
            tox.StatusMessage = "Testing SharpTox";

            tox.Start();

            string id = tox.Id.ToString();
            Console.WriteLine("ID: {0}", id);
            Task.Factory.StartNew(() =>
            {
                while (true) {
                    Thread.Sleep(1000);
                    Console.WriteLine(tox.IsConnected);
                    Console.WriteLine(id);
                }
            });
            Console.ReadKey();
            tox.Dispose();
        }

        //check https://wiki.tox.im/Nodes for an up-to-date list of nodes
        static ToxNode[] Nodes = new ToxNode[]
        {
        new ToxNode("42.159.192.12", 33445, new ToxKey(ToxKeyType.Public, "7F613A23C9EA5AC200264EB727429F39931A86C39B67FC14D9ECA4EBE0D37F25"))
        };

        static void tox_OnFriendMessageReceived(object sender, ToxEventArgs.FriendMessageEventArgs e)
        {
            //get the name associated with the friendnumber
            string name = tox.GetFriendName(e.FriendNumber);

            //print the message to the console
            Console.WriteLine("<{0}> {1}", name, e.Message);
        }

        static void tox_OnFriendRequestReceived(object sender, ToxEventArgs.FriendRequestEventArgs e)
        {
            //automatically accept every friend request we receive
            tox.AddFriendNoRequest(e.PublicKey);
            Console.WriteLine("Received friend req: " + e.PublicKey);
        }
    }
}
