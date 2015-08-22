using SharpTox.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skynet.Base
{
    /// <summary>
    /// 
    /// </summary>
    class Connect
    {
        public ToxKey targetPublicKey { get; set; }
        public int friendId { get; set; }
        private Tox tox;

        public Connect(Tox tox, ToxKey targetPublicKey)
        {
            this.targetPublicKey = targetPublicKey;
            this.tox = tox;
            // add friend if not friend yet
            if((int)ToxErrorFriendByPublicKey.NotFound == tox.GetFriendByPublicKey(targetPublicKey))
            {
                int res = tox.AddFriend(new ToxId(targetPublicKey.GetString(), 100), "hello");
                if(res < 0)
                {
                    Console.WriteLine("Cannot connect with " + targetPublicKey.GetString());
                }
                this.friendId = res;
            }
            
        }

        public Boolean send(string buffer)
        {
            var error = ToxErrorSendMessage.Ok;
            tox.SendMessage(friendId, buffer, ToxMessageType.Message, out error);
            return error == ToxErrorSendMessage.Ok ? true : false;
        }

    }
}
