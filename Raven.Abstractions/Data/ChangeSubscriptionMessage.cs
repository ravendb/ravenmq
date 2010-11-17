using System.Collections.Generic;

namespace Raven.Abstractions.Data
{
    public class ChangeSubscriptionMessage
    {
        public ChangeSubscriptionType Type { get; set; }
        public List<string> Queues { get; set; }

        public ChangeSubscriptionMessage()
        {
            Queues = new List<string>();
        }
    }
}