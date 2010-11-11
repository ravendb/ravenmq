using Raven.Abstractions.Data;
using RavenMQ.Plugins;

namespace RavenMQ.Subscriptions
{
    public class SubscriptionNotifierMessageEnqueueTrigger : AbstractMessageEnqueuedTrigger
    {
        public override void BeforeMessageEnqueued(IncomingMessage message)
        {
            
        }
    }
}