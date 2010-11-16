using System;
using Newtonsoft.Json.Linq;
using RavenMQ.Network;

namespace Raven.MQ.Tests.Network
{
    public class PongServerIntegration : IServerIntegration
    {
        private ServerConnection con;

        public void Init(ServerConnection connection)
        {
            con = connection;
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
    }
}