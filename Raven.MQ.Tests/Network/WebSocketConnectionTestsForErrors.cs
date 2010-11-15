using System;
using System.Net.Sockets;
using System.Text;
using RavenMQ.Network;
using Xunit;

namespace Raven.MQ.Tests.Network
{
    public class WebSocketConnectionTestsForErrors
    {
        [Fact]
        public void Will_get_an_error_when_trying_to_open_a_bad_port()
        {
            using(var server = new WebSocketsServer("localhost", -1))
            {
                Assert.Throws<AggregateException>(() => server.Start().Wait());
            }
        }

        [Fact]
        public void Will_get_an_error_when_trying_to_listen_on_bad_host()
        {
            using (var server = new WebSocketsServer("this-value-does-not-exists.hibernatingrhinos.com", 8181))
            {
                Assert.Throws<AggregateException>(() => server.Start().Wait());
            }
        }
      
        [Fact]
        public void Will_not_crash_if_client_connect_and_immediately_disconnects()
        {
            using (var server = new WebSocketsServer("localhost", 8181))
            {
                server.Start().Wait();
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
            using (var server = new WebSocketsServer("localhost", 8181))
            {
                server.Start().Wait();
                for (int i = 0; i < 5; i++)
                {
                    using (var tcp = new TcpClient())
                    {
                        tcp.Connect("localhost", 8181);
                        var networkStream = tcp.GetStream();
                        var buffer = new byte[526];
                        new Random().NextBytes(buffer);
                        networkStream.Write(buffer, 0, buffer.Length);
                        var bytes = Encoding.UTF8.GetBytes("\r\n\r\n");
                        networkStream.Write(bytes, 0, bytes.Length);
                        networkStream.Flush();
                        networkStream.ReadByte();
                    }
                }
            }
        }

        [Fact]
        public void Can_handle_client_connecting_and_just_hanging_on()
        {
            using (var server = new WebSocketsServer("localhost", 8181))
            {
                server.Start().Wait();
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