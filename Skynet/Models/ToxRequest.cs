﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skynet.Models
{
    public class ToxRequest
    {
        public string url { get; set; }
        public string method { get; set; }
        public string uuid { get; set; }
        public string content { get; set; }
        public string fromNodeId { get; set; }
        public string fromToxId { get; set; }
        public string toNodeId { get; set; }
        public string toToxId { get; set; }
        public long time { get; set; }

        public ToxRequest() {
            time = 0;
        }

        public ToxResponse createResponse(string content = "")
        {
            return new ToxResponse
            {
                url = this.url,
                uuid = this.uuid,
                fromNodeId = this.toNodeId,
                fromToxId = this.toToxId,
                toToxId = this.fromToxId,
                toNodeId = this.toNodeId,
                content = content,
            };
        }
    }
}
