using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RavenMQ.Extensions;

namespace RavenMQ.Subscriptions
{
    public class SubscriptionsSource : ISubscriptionsSource
    {
        private readonly ConcurrentDictionary<Guid, ISubscription> subscriptions = new ConcurrentDictionary<Guid, ISubscription>();

        public Guid AddSubscription(ISubscription subscription)
        {
            Guid key = Guid.NewGuid();
            subscriptions.TryAdd(key, subscription);
            return key;
        }

        public void RemoveSubscription(Guid subscriptionId)
        {
            ISubscription _;
            subscriptions.TryRemove(subscriptionId, out _);
        }

        public void Notify(IEnumerable<SubscriptionFilterInfo> infos)
        {
            subscriptions.Values.Where(subscription => infos.Any(subscription.IsMatch))
                .Apply(x => x.NotifyChanges());
        }
    }
}