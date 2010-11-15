using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace RavenMQ.Network
{
    public class WebSocketClient : IDisposable
    {
        private readonly Uri uri;

        public enum ConnectionStatus
        {
            Connecting,
            Connected,
            Failed
        }

        private readonly Socket socket;
        public ConnectionStatus Status { get; private set; }
        public Exception Exception { get; set; }
        public WebSocketClient(Uri uri)
        {
            this.uri = uri;

            Status = ConnectionStatus.Connecting;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            Task.Factory.FromAsync<string, IPHostEntry>(Dns.BeginGetHostEntry, Dns.EndGetHostEntry, uri.Host, null)
                .ContinueWith(endpointTask =>
                {
                    var endPoint = new IPEndPoint(endpointTask.Result.AddressList.First(),
                                                  uri.Port == 0 ? 8181 : uri.Port
                        );

                    Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, endPoint, null)
                        .ContinueWith(HandshakeWithServer)
                        .ContinueWith(HandleFailure);
                }).ContinueWith(HandleFailure);
            
        }

        private void HandleFailure(Task task)
        {
            if (task.Exception == null)
                return;
            Exception = task.Exception;
            Status = ConnectionStatus.Failed;
        }

        private void HandshakeWithServer(Task t)
        {
            var rand = new Random();
            var challengeBytes = new byte[8];
            rand.NextBytes(challengeBytes);

            var key1 = GenerateKey(rand);
            var key2 = GenerateKey(rand);

            var expectedReply = WebSocketsServer.CalculateSignature(key1, key2, challengeBytes);

            var sb = new StringBuilder()
                .Append("GET ").Append(uri.PathAndQuery).Append(" HTTP/1.1\r\n")
                .Append("Upgrade: WebSocket\r\n")
                .Append("Origin: ").Append(uri.Host).Append(uri.Port == 0 ? 8181 : uri.Port).Append("\r\n")
                .Append("Host: ").Append(uri.Host).Append("\r\n")
                .Append("Sec-Websocket-Key1: ").Append(key1).Append("\r\n")
                .Append("Sec-Websocket-Key2: ").Append(key2).Append("\r\n")
                .Append("Sec-Websocket-Protocol: sample\r\n")
                .Append("\r\n");


            var buffer = new byte[Encoding.UTF8.GetByteCount(sb.ToString()) + 8];
            Encoding.UTF8.GetBytes(sb.ToString(), 0, sb.Length, buffer, 0);
            Array.Copy(challengeBytes, 0, buffer, buffer.Length - 8, 8);

            socket.WriteBuffer(buffer, 1)
                .ContinueWith(_ =>
                {
                    socket.ReadBuffer(1024, Encoding.UTF8.GetBytes("\r\n\r\n"))
                        .ContinueWith(task => ParseServerResponse(expectedReply, task));
                })
                .ContinueWith(HandleFailure);
        }

        private void ParseServerResponse(byte[] expected ,Task<Tuple<Socket, byte[], int>> resultTask)
        {
            var answer = new byte[16];
            Array.Copy(resultTask.Result.Item2, resultTask.Result.Item3 - 16, answer, 0, 16); // copy answer challenge

            for (int i = 0; i < answer.Length; i++)
            {
                if(answer[i] != expected[i])
                    throw new InvalidOperationException("Answer challange did no match expected challange");
            }

            Status = ConnectionStatus.Connected;
        }

        private static string GenerateKey(Random rand)
        {
            var sb = new StringBuilder();
            var spaces = 0;
            var len = rand.Next(15, 25);
            for (int i = 0; i < len; i++)
            {
                switch (rand.Next(3))
                {
                    case 0:
                        var nextDigit = rand.Next('0', '9');
                        sb.Append((char) nextDigit);
                        break;
                    case 1: // digit
                        var nextLetter = rand.Next('A','z');
                        sb.Append((char) nextLetter);
                        break;
                    case 2:
                        sb.Append(' ');
                        spaces++;
                        break;
                }
            }
            if(spaces == 0)// force at least one space
            {
                sb.Insert(sb.Length/2, ' ');
            }
            return sb.ToString();
        }

        public void Dispose()
        {
            if (socket != null)
                socket.Dispose();
        }
    }
}