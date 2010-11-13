using System.Net.Sockets;

namespace RavenMQ.Network
{
    public class WebSocketConnection
    {
        public Socket Socket { get; set; }
        public string Origin { get; set; }
        public string Host { get; set; }
        public string SecWebSocketKey1 { get; set; }
        public string SecWebSocketKey2 { get; set; }
        public string SecWebSocketProtocol { get; set; }
        public string Connection { get; set; }
        public string Location { get; set; }
        public string Method { get; set; }
        public byte[] ChallengeBytes { get; set; }

        public bool IsValid
        {
            get
            {
                return Socket != null &&
                       Host != null &&
                       SecWebSocketKey1 != null &&
                       SecWebSocketKey2 != null &&
                       ChallengeBytes != null &&
                       Location != null;
            }
        }
    }
}