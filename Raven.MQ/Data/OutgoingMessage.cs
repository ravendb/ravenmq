using System;
using Newtonsoft.Json.Linq;

namespace RavenMQ.Data
{
    public class OutgoingMessage
    {
        public Guid Id { get; set; }
        public string Queue { get; set; }
        public JObject Metadata { get; set; }
        public DateTime Expiry { get; set; }
        public byte[] Data { get; set; }
    }
}