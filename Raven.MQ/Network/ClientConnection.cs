using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RavenMQ.Network
{
    public class ClientConnection : IDisposable
    {
        private readonly IPEndPoint endpoint;
        private readonly IClientIntegration clientIntegration;
        private readonly Socket socket;
        private volatile bool connected;

        public ClientConnection(IPEndPoint endpoint, IClientIntegration clientIntegration)
        {
            this.endpoint = endpoint;
            this.clientIntegration = clientIntegration;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            clientIntegration.Init(this);
        }

        public Task Send(JObject msg)
        {
            if(connected == false)
                throw new InvalidOperationException("You cannot call Send before Connect is completed");

            return socket.WriteBuffer(msg);
        }

        public Task Connect()
        {
            var tcs = new TaskCompletionSource<object>();

            Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, endpoint, null)
                .ContinueWith(task =>
                {
                    try
                    {
                        socket.WriteBuffer(new JObject
                        {
                            {"RequestSignature", ServerConnection.RequestHandshakeSignature.ToByteArray()}
                        })
                            .ContinueWith(writeResult =>
                            {
                                if(writeResult.Exception != null)
                                {
                                    tcs.SetException(writeResult.Exception);
                                    return;
                                }
                                socket.ReadJObjectFromBuffer()
                                    .ContinueWith(readTask =>
                                    {
                                        if(readTask.Exception != null)
                                        {
                                            tcs.SetException(readTask.Exception);
                                            return;
                                        }

                                        if (new Guid(readTask.Result.Value<byte[]>("ResponseSignature")) != ServerConnection.ResponseHandshakeSignature)
                                        {
                                            tcs.SetException(new InvalidOperationException("Invalid response signature from server"));
                                            return;
                                        }
                                        connected = true;
                                        tcs.SetResult(null);
                                    }).ContinueWith(_ =>
                                    {
                                        if (connected == false)
                                            return;
                                        StartReceiving();
                                    });
                            });
                    }
                    catch (Exception e)
                    {
                        tcs.SetException(e);
                        throw;
                    }
                });

            return tcs.Task;
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


        public void Dispose()
        {
            Close();
        }
    }
}