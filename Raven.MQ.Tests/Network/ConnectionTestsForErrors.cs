using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RavenMQ.Network;
using Xunit;
using Raven.Abstractions.Extensions;
using RavenMQ.Extensions;

namespace Raven.MQ.Tests.Network
{
    public class ConnectionTestsForErrors
    {
        public class FakeServerIntegration : IServerIntegration
        {
            public void Init(ServerConnection serverConnection)
            {
                
            }

            public void OnNewConnection(Guid connectionId)
            {
                
            }

            public void OnConnectionRemoved(Guid connectionId)
            {
            }

            public void OnClientMessage(Guid connectionId, JObject msg)
            {
                
            }
        }

        [Fact]
        public void Will_not_crash_if_client_connect_and_immediately_disconnects()
        {
            using (var server = new ServerConnection(new IPEndPoint(IPAddress.Loopback, 8181), new FakeServerIntegration()))
            {
                server.Start();
                for (int i = 0; i < 5; i++)
                {
                    using (var tcp = new TcpClient())
                    {
                        tcp.Connect("localhost", 8181);
                    }
                }
            }
        }

        [Fact]
        public void Will_not_crash_if_client_connect_and_send_garbage_data()
        {
            using (var server = new ServerConnection(new IPEndPoint(IPAddress.Loopback, 8181), new FakeServerIntegration()))
            {
                server.Start();
                for (var i = 0; i < 5; i++)
                {
                    using (var tcp = new TcpClient())
                    {
                        tcp.Connect("localhost", 8181);
                        var networkStream = tcp.GetStream();
                        var buffer = new byte[16];
                        new Random(8383).NextBytes(buffer);
                        Array.Copy(BitConverter.GetBytes(12), 0, buffer, 0, 4);
                        networkStream.Write(buffer, 0, buffer.Length);
                        networkStream.Flush();
                        networkStream.Read(buffer, 0, 4);
                        var jObject = networkStream.ToJObject();
                        Assert.Equal("{\"Type\":\"Error\",\"Error\":\"Could not read BSON value\",\"Details\":\"Read past end of current container context.\"}", jObject.ToString(Formatting.None));
                    }
                }
            }
        }

        [Fact]
        public void Can_handle_client_connecting_and_just_hanging_on()
        {
            using (var server = new ServerConnection(new IPEndPoint(IPAddress.Loopback, 8181), new FakeServerIntegration()))
            {
                server.Start();
                using (var tcp = new TcpClient())
                {
                    tcp.Connect("localhost", 8181);

                    using (var tcp2 = new TcpClient())
                    {
                        tcp2.Connect("localhost", 8181);
                    }
                }
            }
        }
    }
}