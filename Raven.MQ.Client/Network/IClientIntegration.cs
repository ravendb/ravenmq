using Newtonsoft.Json.Linq;

namespace Raven.MQ.Client.Network
{
    public interface IClientIntegration
    {
        void Init(ClientConnection connection);
        void OnConnectionClosed();
        void OnMessageArrived(JObject msg);
    }
}