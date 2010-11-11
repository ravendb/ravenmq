namespace RavenMQ.Plugins
{
    public class MessageVeto
    {
        private MessageVeto()
        {
            
        }

        public static MessageVeto Allowed
        {
            get { return new MessageVeto{ IsAllowed = true };}
        }

        public static MessageVeto Denied(string reason)
        {
            return new MessageVeto
            {
                IsAllowed = false,
                Reason = reason
            };
        }

        public bool IsAllowed { get; set; }

        public string Reason { get; set; }
    }
}