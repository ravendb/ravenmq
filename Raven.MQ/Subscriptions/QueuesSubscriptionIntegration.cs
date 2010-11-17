using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RavenMQ.Impl;
using RavenMQ.Network;

namespace RavenMQ.Subscriptions
{
    public class QueuesSubscriptionIntegration : IServerIntegration
    {
        private readonly Queues queues;
        private ServerConnection connection;
        private readonly ConcurrentDictionary<Guid,Guid> connectionIdToSubscriptionId = new ConcurrentDictionary<Guid, Guid>();
        private readonly ConcurrentDictionary<Guid, ClientSubscription> connectionIdToClientSubscription = new ConcurrentDictionary<Guid, ClientSubscription>();

        public QueuesSubscriptionIntegration(Queues queues)
        {
            this.queues = queues;
        }

        public void Init(ServerConnection serverConnection)
        {
            connection = serverConnection;
        }

        public void OnNewConnection(Guid connectionId)
        {
            var clientSubscription = new ClientSubscription(connectionId, connection);
            connectionIdToClientSubscription.TryAdd(connectionId, clientSubscription);
            var subscriptionId = queues.Subscriptions.AddSubscription(clientSubscription);
            connectionIdToSubscriptionId.TryAdd(connectionId, subscriptionId);
        }

        public void OnConnectionRemoved(Guid connectionId)
        {
            ClientSubscription _;
            connectionIdToClientSubscription.TryRemove(connectionId, out _);
            
            Guid subscriptionId;
            if (connectionIdToSubscriptionId.TryGetValue(connectionId, out subscriptionId) == false)
                return;

            queues.Subscriptions.RemoveSubscription(subscriptionId);
        }

        public void OnClientMessage(Guid connectionId, JObject msg)
        {
            var subscriptionMessage = new JsonSerializer().Deserialize<ChangeSubscriptionMessage>(new JTokenReader(msg));

            ClientSubscription subscription;
            if (connectionIdToClientSubscription.TryGetValue(connectionId, out subscription) == false)
                return;

            subscription.HandleSubscriptionMesage(subscriptionMessage);
        }
    }
}