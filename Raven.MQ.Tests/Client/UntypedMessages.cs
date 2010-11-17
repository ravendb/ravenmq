using System;
using System.Linq;
using System.Net;
using System.Threading;
using Raven.Abstractions.Data;
using Raven.Http;
using Raven.MQ.Client;
using Raven.MQ.Server;
using RavenMQ.Config;
using RavenMQ.Subscriptions;
using Xunit;

namespace Raven.MQ.Tests.Client
{
    public class UntypedMessages : IDisposable
    {
        private readonly RavenMqServer ravenMqServer;
        private readonly InMemoryRavenConfiguration configuration;

        public UntypedMessages()
        {
            configuration = new RavenConfiguration
            {
                RunInMemory = true,
                AnonymousUserAccessMode = AnonymousUserAccessMode.All
            };
            ravenMqServer = new RavenMqServer(configuration);
        }

        [Fact]
        public void Can_get_message_from_client_connection()
        {
            using(var connection = new RavenMQConnection(new Uri(configuration.ServerUrl), new IPEndPoint(IPAddress.Loopback, 8181)))
            {
                var manualResetEventSlim = new ManualResetEventSlim(false);
                OutgoingMessage msg = null;
                connection.Subscribe("/queues/abc", (context, message) =>
                {
                    msg = message;
                    manualResetEventSlim.Set();
                });

                WaitForSubscription();

                ravenMqServer.Queues.Enqueue(new IncomingMessage
                {
                    Data = new byte[] {1, 2, 3},
                    Queue = "/queues/abc"
                });

                manualResetEventSlim.Wait();

                Assert.Equal(new byte[]{1,2,3}, msg.Data);
            }

        }

        private void WaitForSubscription()
        {
            var subscriptionsSource = ((SubscriptionsSource)ravenMqServer.Queues.Subscriptions);
            while(subscriptionsSource.Subscriptions.Count() == 0)
            {
                Thread.Sleep(1);
            }
            var subscription = (ClientSubscription)subscriptionsSource.Subscriptions.First();
            while (subscription.Subscriptions.Count() == 0)
            {
                Thread.Sleep(1);
            }
        }

        public void Dispose()
        {
            ravenMqServer.Dispose();
        }
    }
}