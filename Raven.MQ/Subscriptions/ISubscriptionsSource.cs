using System;
using System.Collections.Generic;

namespace RavenMQ.Subscriptions
{
    public interface ISubscriptionsSource
    {
        Guid AddSubscription(ISubscription subscription);
        void RemoveSubscription(Guid subscriptionId);
        void Notify(IEnumerable<SubscriptionFilterInfo> infos);
    }
}