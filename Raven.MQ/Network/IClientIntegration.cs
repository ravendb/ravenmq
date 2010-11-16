using Newtonsoft.Json.Linq;

namespace RavenMQ.Network
{
    public interface IClientIntegration
    {
        void Init(ClientConnection connection);
        void OnConnectionClosed();
        void OnMessageArrived(JObject msg);
    }
}