using System;

namespace RavenMQ.Network
{
    public interface IServerIntegration
    {
        void Init(ServerConnection connection);
        void OnNewConnection(Guid connectionId);
        void OnConnectionRemoved(Guid connectionId);
    }
}