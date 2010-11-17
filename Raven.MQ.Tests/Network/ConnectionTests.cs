using System;
using System.Net;
using Newtonsoft.Json;
using RavenMQ.Network;
using Xunit;

namespace Raven.MQ.Tests.Network
{
    public class ConnectionTests : IDisposable
    {
        private readonly ServerConnection connection;

        public ConnectionTests()
        {
            connection = new ServerConnection(new IPEndPoint(IPAddress.Loopback, 8181), new PongServerIntegration());
        }

        [Fact]
        public void Can_get_notificaton_from_server()
        {
            connection.Start();

            var clientIntegration = new CaptureClientIntegration();
            using(var clientConnection = new ClientConnection(new IPEndPoint(IPAddress.Loopback, 8181), clientIntegration))
            {
                clientConnection.Connect().Wait();

                clientIntegration.MessageArrived.WaitOne();

                Assert.Equal("{\"Pong\":\"Ping\"}", clientIntegration.Msgs[0].ToString(Formatting.None));
            }
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}