using System;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;
using Raven.Abstractions.Data;
using Raven.MQ.Client.Network;
using Raven.MQ.Server;
using RavenMQ.Config;
using RavenMQ.Impl;
using RavenMQ.Network;
using RavenMQ.Subscriptions;
using Xunit;
using System.Linq;

namespace Raven.MQ.Tests.Network
{
    public class SubscriptionTests : IDisposable
    {
        private readonly RavenMqServer ravenMqServer;
        private readonly InMemoryRavenConfiguration configuration;

        public SubscriptionTests()
        {
            configuration = new InMemoryRavenConfiguration
            {
                RunInMemory = true
            };
            configuration.Initialize();
            ravenMqServer = new RavenMqServer(configuration);
        }

        [Fact]
        public void Can_subscribe_and_get_notification_about_new_messages()
        {
            var captureClientIntegration = new CaptureClientIntegration();
            using(var c = new ClientConnection(new IPEndPoint(IPAddress.Loopback, 8181), captureClientIntegration))
            {
                c.Connect().Wait();

                c.Send(JObject.FromObject(new ChangeSubscriptionMessage
                {
                    Queues = {"/queues/abc"},
                    Type = ChangeSubscriptionType.Add
                })).Wait();

                WaitForSubscription();

                ravenMqServer.Queues.Enqueue(new IncomingMessage
                {
                    Queue = "/queues/abc",
                    Data = new byte[]{12,3},
                });
                captureClientIntegration.MessageArrived.WaitOne();

                Assert.True(captureClientIntegration.Msgs[0].Value<bool>("Changed"));
            }
        }

        private void WaitForSubscription()
        {
            var subscriptionsSource = ((SubscriptionsSource)ravenMqServer.Queues.Subscriptions);
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