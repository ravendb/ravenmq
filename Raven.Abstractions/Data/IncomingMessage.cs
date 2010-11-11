using System;
using Newtonsoft.Json.Linq;

namespace Raven.Abstractions.Data
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