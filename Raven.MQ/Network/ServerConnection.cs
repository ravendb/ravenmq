using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace RavenMQ.Network
{
    public class ServerConnection : IDisposable
    {
        public static readonly Guid RequestHandshakeSignature = new Guid("585D6B31-A06A-40DD-99EE-001323DAADB0");
        public static readonly Guid ResponseHandshakeSignature = new Guid("3397D9BF-2C51-448E-A1B1-3981D42A8609");
        private readonly ConcurrentDictionary<Guid, Socket> connections = new ConcurrentDictionary<Guid, Socket>();
        private readonly IPEndPoint endpoint;
        private readonly Socket listener;

        public ServerConnection(IPEndPoint endpoint)
        {
            this.endpoint = endpoint;
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
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
            listener.Bind(endpoint);
            listener.Listen(10);

            ListenForConnections();
        }

        public void Send(Guid id, JObject msg)
        {
            Socket value;
            if (connections.TryGetValue(id, out value) == false)
                return;

            value.WriteBuffer(msg).ContinueWith(task =>
            {
                if (task.Exception == null) 
                    return;
                RemoveConnection(id);
            });
        }

        private void RemoveConnection(Guid id)
        {
            Socket value;
            if (connections.TryRemove(id, out value) == false)
                return;
            value.Dispose();
        }

        private void ListenForConnections()
        {
            Task.Factory.FromAsync<Socket>(listener.BeginAccept, listener.EndAccept, null)
                .ContinueWith(task =>
                {
                    ListenForConnections();

                    Handshake(task.Result);
                });
        }

        private void Handshake(Socket socket)
        {
            socket.ReadJObjectFromBuffer()
                .ContinueWith(initMsgTask =>
                {
                    JObject result;
                    try
                    {
                        result = initMsgTask.Result;
                    }
                    catch (AggregateException e)
                    {
                        socket.WriteBuffer(new JObject
                        {
                            {"Type", "Error"},
                            {"Error", "Could not read BSON value"},
                            {"Details", string.Join(Environment.NewLine, e.InnerExceptions.Select(x=>x.Message))}
                        })
                            .ContinueWith(_ => socket.Dispose());
                        return;
                    }
                    catch (Exception e)
                    {
                        socket.WriteBuffer(new JObject
                        {
                            {"Type", "Error"},
                            {"Error", "Could not read BSON value"},
                            {"Details", e.Message}
                        })
                            .ContinueWith(_ => socket.Dispose());
                        return;
                    }
                    if (new Guid(result.Value<byte[]>("RequestSignature")) != RequestHandshakeSignature)
                    {
                        socket.WriteBuffer(new JObject
                        {
                            {"Type", "Error"},
                            {"Error", "Invalid server signature"}
                        })
                            .ContinueWith(_ => socket.Dispose());
                        return;
                    }

                    socket.WriteBuffer(new JObject
                    {
                        {"Type", "Confirmation"},
                        {"ResponseSignature", ResponseHandshakeSignature.ToByteArray()}
                    })
                        .ContinueWith(responseMsgTask =>
                        {
                            if (responseMsgTask.Exception != null)
                            {
                                socket.Dispose();
                                return;
                            }
                            AddConnection(socket);
                        });
                })
                    .ContinueWith(overallResponseTask =>
                    {
                        if (overallResponseTask.Exception != null)
                            socket.Dispose();
                    });
        }

        private void AddConnection(Socket socket)
        {
            connections.TryAdd(Guid.NewGuid(), socket);
        }
    }
}