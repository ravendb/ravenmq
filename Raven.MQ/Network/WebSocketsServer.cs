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

        private static readonly Regex httpParameters = new Regex(@"([^\s]+)\s+([^\s]+)\s+([^\s]+)\s*",
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

        public void Start()
        {
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            Task.Factory.FromAsync<string, IPAddress[]>(Dns.BeginGetHostAddresses, Dns.EndGetHostAddresses, host, null)
                .ContinueWith(addressesTask =>
                {
                    foreach (var ipAddress in addressesTask.Result)
                    {
                        listener.Bind(new IPEndPoint(ipAddress, port));
                    }
                    ListenForReuqests();
                });
        }

        private Task ListenForReuqests()
        {
            return Task.Factory.FromAsync<Socket>(listener.BeginAccept, listener.EndAccept, null)
                .ContinueWith(socketTask =>
                {
                    ListenForReuqests(); // starts listening for new requests

                    var socket = socketTask.Result;
                    ReadBuffer(socket, 1024 /* the headshake is smaller than 1,024 bytes */)
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

            int result1 = CalculatePartialResult(arg.Result.SecWebSocketKey1);
            int result2 = CalculatePartialResult(arg.Result.SecWebSocketKey2);

            var buffer = new byte[16];

            Array.Copy(buffer, 0, BitConverter.GetBytes(result1), 0, 4);
            Array.Copy(buffer, 4, BitConverter.GetBytes(result2), 0, 4);
            Array.Copy(buffer, webSocketConnection.ChallengeBytes, 8);


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


            return WriteBuffer(webSocketConnection, buffer);
        }

        private static int CalculatePartialResult(IEnumerable<char> secWebSocket)
        {
            var numberAsString = secWebSocket.Where(char.IsDigit).Aggregate(new StringBuilder(), (sb, ch) => sb.Append(ch)).ToString();
            return int.Parse(numberAsString) / secWebSocket.Count(ch => ch == ' ');
        }

        private static WebSocketConnection ParseClientRequest(Task<Tuple<Socket, byte[], int>> bufferTask)
        {
            var clientRequest = Encoding.UTF8.GetString(bufferTask.Result.Item2, 0,
                                                        bufferTask.Result.Item3 - 8
                /* the last 8 bytes are for signature */);
            var matches = httpFields.Matches(clientRequest);
            var con = new WebSocketConnection
            {
                Socket = bufferTask.Result.Item1,
            };
            var httpParaemtersMatch = httpParameters.Match(clientRequest);
            if (httpParaemtersMatch.Success)
            {
                con.Method = httpParaemtersMatch.Groups[1].Value;
                con.Location = httpParaemtersMatch.Groups[2].Value;
            }
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
            Array.Copy(bufferTask.Result.Item2, bufferTask.Result.Item3 - 8, con.ChallengeBytes, 0, 8);
            return con;
        }

        private static Task<Tuple<Socket, byte[], int>> ReadBuffer(Socket socket, int bufferSize)
        {
            var completionSource = new TaskCompletionSource<Tuple<Socket, byte[], int>>();
            var buffer = new byte[bufferSize];
            var start = 0;
            AsyncCallback callback = null;
            callback = ar =>
            {
                int read;
                try
                {
                    read = socket.EndReceive(ar);
                    start += read;
                }
                catch (Exception e)
                {
                    completionSource.SetException(e);
                    return;
                }
                if (read == 0)
                {
                    completionSource.SetResult(Tuple.Create(socket, buffer, start));
                }
                socket.BeginReceive(buffer, start, bufferSize - start, SocketFlags.None, callback, null);
            };
            socket.BeginReceive(buffer, start, bufferSize - start, SocketFlags.None, callback, null);

            return completionSource.Task;
        }

        private static Task<WebSocketConnection> WriteBuffer(WebSocketConnection webSocketConnection, byte[] buffer)
        {
            var completionSource = new TaskCompletionSource<WebSocketConnection>();
            var start = 0;
            AsyncCallback callback = null;
            callback = ar =>
            {
                int read;
                try
                {
                    read = webSocketConnection.Socket.EndSend(ar);
                    start += read;
                }
                catch (Exception e)
                {
                    completionSource.SetException(e);
                    return;
                }
                if (read == 0)
                {
                    completionSource.SetResult(webSocketConnection);
                }
                webSocketConnection.Socket.BeginSend(buffer, start, buffer.Length - start, SocketFlags.None, callback, null);
            };
            webSocketConnection.Socket.BeginSend(buffer, start, buffer.Length - start, SocketFlags.None, callback, null);

            return completionSource.Task;
        }
    }
}