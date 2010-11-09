using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;

namespace RavenMQ
{
    public class IncomingMessage
    {
        public IncomingMessage()
        {
            TimeToLive = TimeSpan.FromMinutes(5);
            Metadata = new JObject();
        }
        public string Queue { get; set; }

        public TimeSpan TimeToLive { get; set; }

        public JObject Metadata { get; set; }

        public byte[] Data { get; set; }
    }
}