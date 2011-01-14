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
			NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(configuration.Port);
            ravenMqServer = new RavenMqServer(configuration);
        }

        [Fact]
        public void Can_get_message_from_client_connection()
        {
            using(var connection = new RavenMQConnection(new Uri(configuration.ServerUrl), new IPEndPoint(IPAddress.Loopback, 8182)))
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

		[Fact]
		public void Publishing_from_the_client_out_of_scope()
		{
			using (var connection = new RavenMQConnection(new Uri(configuration.ServerUrl), new IPEndPoint(IPAddress.Loopback, 8182)))
			{

				connection.PublishAsync(new IncomingMessage
			{	
					Queue = "/queues/abc",
					Data = new byte[] {1, 2, 3},
				});
				var manualResetEventSlim = new ManualResetEventSlim(false);
				OutgoingMessage msg = null;
				connection.Subscribe("/queues/abc", (context, message) =>
				{
					msg = message;
					manualResetEventSlim.Set();
				});

				manualResetEventSlim.Wait(TimeSpan.FromSeconds(10));

				Assert.Equal(new byte[] { 1, 2, 3 }, msg.Data);
			}
		}

        [Fact]
        public void Can_send_a_message_from_receiving_msg()
        {
            using (var connection = new RavenMQConnection(new Uri(configuration.ServerUrl), new IPEndPoint(IPAddress.Loopback, 8182)))
            {
                var manualResetEventSlim = new ManualResetEventSlim(false);
                OutgoingMessage msg = null;
                connection.Subscribe("/queues/abc", (context, message) =>
                {
                    msg = message;
                    context.Send(new IncomingMessage
                    {
                        Data = new byte[] {4, 5, 6},
                        Queue = "/queues/def"
                    });
                    manualResetEventSlim.Set();
                });

                WaitForSubscription();

                ravenMqServer.Queues.Enqueue(new IncomingMessage
                {
                    Data = new byte[] { 1, 2, 3 },
                    Queue = "/queues/abc"
                });

                manualResetEventSlim.Wait();

                Assert.Equal(new byte[] { 1, 2, 3 }, msg.Data);

                ReadResults results;
                do
                {
                    results = ravenMqServer.Queues.Read(new ReadRequest
                    {
                        Queue = "/queues/def"
                    });
                    Thread.Sleep(1);
                } while (results.Results.Count() == 0);

                Assert.Equal(new byte[]{4,5,6}, results.Results.First().Data);
            }
        }

        [Fact]
        public void Can_flush_messages_manually()
        {
            using (var connection = new RavenMQConnection(new Uri(configuration.ServerUrl), new IPEndPoint(IPAddress.Loopback, 8182)))
            {
            	connection.ConnectToServerTask.Wait();
                var manualResetEventSlim = new ManualResetEventSlim(false);
                OutgoingMessage msg = null;
                connection.Subscribe("/queues/abc", (context, message) =>
                {
                    msg = message;
                    context.Send(new IncomingMessage
                    {
                        Data = new byte[] { 4, 5, 6 },
                        Queue = "/queues/def"
                    });
                    context.FlushAsync().Wait();
                    manualResetEventSlim.Set();
                });

                WaitForSubscription();

                ravenMqServer.Queues.Enqueue(new IncomingMessage
                {
                    Data = new byte[] { 1, 2, 3 },
                    Queue = "/queues/abc"
                });

                manualResetEventSlim.Wait();

                Assert.Equal(new byte[] { 1, 2, 3 }, msg.Data);

                var results = ravenMqServer.Queues.Read(new ReadRequest
                {
                    Queue = "/queues/def"
                });

                Assert.Equal(new byte[] { 4, 5, 6 }, results.Results.First().Data);
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