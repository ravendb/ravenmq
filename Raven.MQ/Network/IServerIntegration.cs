using System;
using Newtonsoft.Json.Linq;

namespace RavenMQ.Network
{
    public interface IServerIntegration
    {
        void Init(ServerConnection serverConnection);
        void OnNewConnection(Guid connectionId);
        void OnConnectionRemoved(Guid connectionId);
        void OnClientMessage(Guid connectionId, JObject msg);
    }
}