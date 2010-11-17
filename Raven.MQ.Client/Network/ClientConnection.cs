using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Raven.Abstractions.Extensions;

namespace Raven.MQ.Client.Network
{
    public class ClientConnection : IDisposable
    {
        public static readonly Guid RequestHandshakeSignature = new Guid("585D6B31-A06A-40DD-99EE-001323DAADB0");
        public static readonly Guid ResponseHandshakeSignature = new Guid("3397D9BF-2C51-448E-A1B1-3981D42A8609");
        
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

            return socket.Write(msg);
        }

        public Task Connect()
        {
            return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, endpoint, null)
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                        return task;

                    return socket.Write(new JObject
                    {
                        {"RequestSignature", RequestHandshakeSignature.ToByteArray()}
                    })
                        .IgnoreExceptions()
                        .ContinueWith(writeResult =>
                                      socket.ReadJObjectFromBuffer()
                                          .ContinueWith(AssertValidServerResponse)
                                          .ContinueWith(connectionTask =>
                                          {
                                              if (connectionTask.Exception != null)
                                                  return;
                                              StartReceiving();
                                              connected = true;
                                          }))
                        .Unwrap();

                }).Unwrap();
        }

        private static void AssertValidServerResponse(Task<JObject> readTask)
        {
            if (new Guid(readTask.Result.Value<byte[]>("ResponseSignature")) !=
                ResponseHandshakeSignature)
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