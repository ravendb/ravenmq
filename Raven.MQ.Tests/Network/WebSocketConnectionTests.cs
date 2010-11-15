using System;
using System.IO;
using System.Net.Sockets;
using RavenMQ.Network;
using Xunit;

namespace Raven.MQ.Tests.Network
{
    public class WebSocketConnectionTests : IDisposable
    {
        private readonly WebSocketsServer server;

        public WebSocketConnectionTests()
        {
            server = new WebSocketsServer("loaclhost", 8181);
            server.Start();
        }

        [Fact]
        public void Can_connect_to_server()
        {
            using(var tcp = new TcpClient())
            {
                tcp.Connect("localhost", 8181);
                var sw = new StreamWriter(tcp.GetStream());
                sw.WriteLine("GET /foo HTTP/1.1");
                sw.WriteLine("Connection: Upgrade");
                sw.WriteLine("Host: localhost");

                Assert.False(true);
            }

        }


        public void Dispose()
        {
            server.Dispose();
        }
    }
}