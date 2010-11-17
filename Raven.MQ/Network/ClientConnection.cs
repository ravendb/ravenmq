using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RavenMQ.Extensions;

namespace RavenMQ.Network
{
    public class ClientConnection : IDisposable
    {
        private readonly IClientIntegration clientIntegration;
        private readonly IPEndPoint endpoint;
        private readonly Socket socket;
        private volatile bool connected;

        public ClientConnection(IPEndPoint endpoint, IClientIntegration clientIntegration)
        {
            this.endpoint = endpoint;
            this.clientIntegration = clientIntegration;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            clientIntegration.Init(this);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion

        public Task Send(JObject msg)
        {
            if (connected == false)
                throw new InvalidOperationException("You cannot call Send before Connect is completed");

            return socket.WriteBuffer(msg);
        }

        public Task Connect()
        {
            return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, endpoint, null)
                .ContinueWith(task => socket.WriteBuffer(new JObject
                {
                    {"RequestSignature", ServerConnection.RequestHandshakeSignature.ToByteArray()}
                })
                                          .IgnoreExceptions()
                                          .ContinueWith(writeResult =>
                                                        socket.ReadJObjectFromBuffer()
                                                            .IgnoreExceptions()
                                                            .ContinueWith(AssertValidServerResponse)
                                                            .IgnoreExceptions()
                                                            .ContinueWith(connectionTask =>
                                                            {
                                                                if (connectionTask.Exception == null)
                                                                    StartReceiving();
                                                            }))
                                           .Unwrap()
                ).Unwrap();
        }

        private static void AssertValidServerResponse(Task<JObject> readTask)
        {
            if (new Guid(readTask.Result.Value<byte[]>("ResponseSignature")) !=
                ServerConnection.ResponseHandshakeSignature)
                throw new InvalidOperationException("Invalid response signature from server");
        }

        private void StartReceiving()
        {
            socket.ReadJObjectFromBuffer()
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        Close();
                        return;
                    }

                    StartReceiving();

                    clientIntegration.OnMessageArrived(task.Result);
                });
        }

        private void Close()
        {
            socket.Dispose();
            clientIntegration.OnConnectionClosed();
        }
    }
}