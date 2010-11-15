using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using RavenMQ.Network;
using Xunit;

namespace Raven.MQ.Tests.Network
{
    public class WebSocketConnectionTests : IDisposable
    {
        private readonly WebSocketsServer server;

        public WebSocketConnectionTests()
        {
            server = new WebSocketsServer("localhost", 8181);
            server.Start().Wait();
        }

        [Fact]
        public void Can_connect_to_server()
        {
            using(var tcp = new TcpClient())
            {
                tcp.Connect("localhost", 8181);
                var networkStream = tcp.GetStream();
                var sw = new StreamWriter(networkStream);
                sw.Write("GET /demo HTTP/1.1\r\n");
                sw.Write("Host: example.com\r\n");
                sw.Write("Connection: Upgrade\r\n");
                sw.Write("Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n");
                sw.Write("Sec-WebSocket-Protocol: sample\r\n");
                sw.Write("Upgrade: WebSocket\r\n");
                sw.Write("Sec-WebSocket-Key1: 4 @1  46546xW%0l 1 5\r\n");
                sw.Write("Origin: http://example.com\r\n");
                sw.Write("\r\n");
                sw.Flush();
                networkStream.Flush();

                networkStream.Write(new byte[] {2, 4, 5, 6, 7, 8, 9, 1}, 0, 8);

                networkStream.Flush();

                var sr = new StreamReader(networkStream);
                var sb = new StringBuilder();
                string line;
                while((line = sr.ReadLine()) == "")
                {
                    sb.AppendLine(line);
                }

                Assert.Equal("", sb.ToString());
            }

        }


        public void Dispose()
        {
            server.Dispose();
        }
    }
}