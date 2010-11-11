using Raven.Abstractions.Data;
using RavenMQ.Plugins;
using System.Linq;

namespace RavenMQ.Subscriptions
{
    public class SubscriptionNotifierMessageEnqueueTrigger : AbstractMessageEnqueuedTrigger
    {
        public override void AfterCommit(System.Collections.Generic.IEnumerable<IncomingMessage> messages)
        {
            var subscriptionFilterInfos = messages.Select(msg => new SubscriptionFilterInfo
            {
                Metadata = msg.Metadata,
                Queue = msg.Queue
            }).ToArray();

            Queues.Subscriptions.Notify(subscriptionFilterInfos);
        }
    }
}