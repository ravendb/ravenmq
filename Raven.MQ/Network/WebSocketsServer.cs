using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;

namespace RavenMQ.Network
{
    public class WebSocketsServer : IDisposable
    {
        private static readonly Regex httpFields = new Regex(@"([^:\s]+):\s([^\r\n]+)",
                                                             RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex httpParameters = new Regex(@"([^\s]+)\s+([^\s]+)\s+(HTTP/1\.[10])\s*",
                                                                 RegexOptions.Compiled | RegexOptions.Singleline);

        private ConcurrentBag<WebSocketConnection> socketConnections = new ConcurrentBag<WebSocketConnection>();

        private readonly string host;
        private readonly int port;
        private Socket listener;

        public WebSocketsServer(string host, int port)
        {
            this.host = host;
            this.port = port;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (listener != null)
                listener.Dispose();
        }

        #endregion

        public Task Start()
        {
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            return Task.Factory.FromAsync<string, IPAddress[]>(Dns.BeginGetHostAddresses, Dns.EndGetHostAddresses, host, null)
                .ContinueWith(addressesTask =>
                {
                    foreach (var ipAddress in addressesTask.Result)
                    {
                        if (ipAddress.AddressFamily != AddressFamily.InterNetworkV6)
                            listener.Bind(new IPEndPoint(ipAddress, port));
                    }

                    listener.Listen(10);

                    ListenForReuqests(listener);
                });
        }

        private Task ListenForReuqests(Socket socket)
        {
            return Task.Factory.FromAsync<Socket>(socket.BeginAccept, socket.EndAccept, null)
                .ContinueWith(socketTask =>
                {
                    ListenForReuqests(socket); // starts listening for new requests

                    socketTask.Result.ReadBuffer(1024 /* the headshake is smaller than 1,024 bytes */, Encoding.UTF8.GetBytes("\r\n\r\n"))
                        .ContinueWith<WebSocketConnection>(ParseClientRequest)
                        .ContinueWith(task =>
                        {
                            SendServerResponse(task)
                                .ContinueWith(AddConnection);
                        });
                });
        }

        private void AddConnection(Task<WebSocketConnection> webSocketConnectionTask)
        {
            socketConnections.Add(webSocketConnectionTask.Result);
        }

        private static Task<WebSocketConnection> SendServerResponse(Task<WebSocketConnection> arg)
        {
            var webSocketConnection = arg.Result;

            byte[] buffer = CalculateSignature(arg.Result.SecWebSocketKey1, arg.Result.SecWebSocketKey2, webSocketConnection.ChallengeBytes);


            var sb = new StringBuilder();
            sb.Append("HTTP/1.1 101 Web Socket Protocol Handshake\r\n")
                .Append("Upgrade: WebSocket\r\n")
                .Append("Connection: Upgrade\r\n")
                .Append("Sec-WebSocket-Origin: ").Append(webSocketConnection.Origin).Append("\r\n")
                .Append("Sec-WebSocket-Location: ").Append(webSocketConnection.Location).Append("\r\n");
            if (webSocketConnection.SecWebSocketProtocol != null)
            {
                sb.Append("Sec-WebSocket-Protocol: ").Append(webSocketConnection.SecWebSocketProtocol).Append("\r\n");
            }
            sb.Append("\r\n");

            var bytes = new List<byte>(Encoding.UTF8.GetBytes(sb.ToString()));
            using (var md5 = MD5.Create())
                bytes.AddRange(md5.ComputeHash(buffer));


            return webSocketConnection.Socket.WriteBuffer(bytes.ToArray(), webSocketConnection);
        }

        public static byte[] CalculateSignature(string secWebSocketKey1, string secWebSocketKey2, byte[] challengeBytes)
        {
            int result1 = CalculatePartialResult(secWebSocketKey1);
            int result2 = CalculatePartialResult(secWebSocketKey2);

            var buffer = new byte[16];

            Array.Copy(BitConverter.GetBytes(result1), 0, buffer, 0, 4);
            Array.Copy(BitConverter.GetBytes(result2), 0, buffer, 4, 4);
            Array.Copy(challengeBytes, buffer, 8);
            return buffer;
        }

        private static int CalculatePartialResult(IEnumerable<char> secWebSocket)
        {
            var numberAsString = secWebSocket.Where(char.IsDigit).Aggregate(new StringBuilder(), (sb, ch) => sb.Append(ch)).ToString();
            return (int) (long.Parse(numberAsString)/secWebSocket.Count(ch => ch == ' '));
        }

        private static WebSocketConnection ParseClientRequest(Task<Tuple<Socket, byte[], int>> bufferTask)
        {
            var buferSize = bufferTask.Result.Item3;
            var buffer = bufferTask.Result.Item2;
            var socket = bufferTask.Result.Item1;
            if(buferSize <= 8) // request too small
            {
                FailRequest(socket);
                return null;
            }
            var con = new WebSocketConnection
            {
                Socket = bufferTask.Result.Item1,
            };
            
            var clientRequest = Encoding.UTF8.GetString(buffer, 0,
                                                        buferSize - 8
                /* the last 8 bytes are for signature */);
            var firstLine = clientRequest.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if(firstLine == null)
            {
                FailRequest(socket);
                return null;
            }
            var httpParaemtersMatch = httpParameters.Match(firstLine);
            if (httpParaemtersMatch.Success)
            {
                con.Method = httpParaemtersMatch.Groups[1].Value;
                switch (con.Method)
                {
                    case "GET":
                    case "POST":
                    case "PUT":
                    case "DELETE":
                        break;
                    default:
                        FailRequest(socket);
                        return null;
                }
                con.Location = httpParaemtersMatch.Groups[2].Value;
            }
            else
            {
                FailRequest(socket);
                return null;
            }
            var matches = httpFields.Matches(clientRequest);
            foreach (Match match in matches)
            {
                switch (match.Groups[1].Value.ToLowerInvariant())
                {
                    case "connection":
                        con.Connection = match.Groups[2].Value;
                        break;
                    case "host":
                        con.Host = match.Groups[2].Value;
                        break;
                    case "sec-websocket-key1":
                        con.SecWebSocketKey1 = match.Groups[2].Value;
                        break;
                    case "sec-websocket-protocol":
                        con.SecWebSocketProtocol = match.Groups[2].Value;
                        break;
                    case "sec-websocket-key2":
                        con.SecWebSocketKey2 = match.Groups[2].Value;
                        break;
                    case "origin":
                        con.Origin = match.Groups[2].Value;
                        break;
                }
            }
            con.ChallengeBytes = new byte[8];
            Array.Copy(bufferTask.Result.Item2, buferSize - 8, con.ChallengeBytes, 0, 8);
            return con;
        }

        private static void FailRequest(Socket socket)
        {
            socket.WriteBuffer(Encoding.UTF8.GetBytes(@"400 Bad Request\r\n\rn<p>Cannot understand request</p>"), 1)
                .ContinueWith(task => socket.Dispose());
        }
    }
}