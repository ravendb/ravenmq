using Newtonsoft.Json.Linq;

namespace RavenMQ.Subscriptions
{
    public class SubscriptionFilterInfo
    {
        public string Queue { get; set; }
        public JObject Metadata { get; set; }
    }
}