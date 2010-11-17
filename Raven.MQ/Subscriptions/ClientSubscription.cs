using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RavenMQ.Network;

namespace RavenMQ.Subscriptions
{
    public class ClientSubscription : ISubscription
    {
        private readonly ConcurrentDictionary<string, string> subscriptions = new ConcurrentDictionary<string, string>();
        private readonly Guid clientId;
        private readonly ServerConnection connection;

        public ClientSubscription(Guid clientId, ServerConnection connection)
        {
            this.clientId = clientId;
            this.connection = connection;
        }

        public IEnumerable<string> Subscriptions
        {
            get { return subscriptions.Keys; }
        }

        public void HandleSubscriptionMesage(ChangeSubscriptionMessage msg)
        {
            switch (msg.Type)
            {
                case ChangeSubscriptionType.Add:
                    foreach (var queue in msg.Queues)
                    {
                        subscriptions.TryAdd(queue, queue);
                    }
                    break;
                case ChangeSubscriptionType.Remove:
                    foreach (var queue in msg.Queues)
                    {
                        string _;
                        subscriptions.TryRemove(queue, out _);
                    }
                    break;
            }
        }
        public bool IsMatch(SubscriptionFilterInfo subscriptionFilterInfo)
        {
            return subscriptions.ContainsKey(subscriptionFilterInfo.Queue);
        }

        public void NotifyChanges()
        {
            connection.Send(clientId, JObject.FromObject(new
            {
                Changed = true
            }));
        }
    }
}