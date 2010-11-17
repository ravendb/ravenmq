using System;
using Newtonsoft.Json.Linq;
using RavenMQ.Network;

namespace Raven.MQ.Tests.Network
{
    public class PongServerIntegration : IServerIntegration
    {
        private ServerConnection con;

        public void Init(ServerConnection serverConnection)
        {
            con = serverConnection;
        }

        public void OnNewConnection(Guid connectionId)
        {
            con.Send(connectionId, new JObject
            {
                {"Pong", "Ping"}
            });
        }

        public void OnConnectionRemoved(Guid connectionId)
        {
        }

        public void OnClientMessage(Guid connectionId, JObject msg)
        {
            
        }
    }
}