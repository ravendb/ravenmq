using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;
using RavenMQ.Network;

namespace Raven.MQ.Tests.Network
{
    public class CaptureClientIntegration : IClientIntegration
    {
        public List<JObject> Msgs = new List<JObject>();

        public ManualResetEvent MessageArrived = new ManualResetEvent(false);

        public void Init(ClientConnection connection)
        {
        }

        public void OnConnectionClosed()
        {
        }

        public void OnMessageArrived(JObject msg)
        {
            Msgs.Add(msg);
            MessageArrived.Set();
        }
    }
}