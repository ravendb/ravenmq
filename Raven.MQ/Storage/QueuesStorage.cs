using System;
using Raven.Munin;

namespace RavenMQ.Storage
{
    public class QueuesStorage : Database
    {
        public QueuesStorage(IPersistentSource persistentSource) : base(persistentSource)
        {
            Messages = Add(new Table(key => key["MsgId"], "Messages")
            {
                {"ByQueueName", key => key.Value<string>("QueueName")},
                {"ByTime", key => key.Value<DateTime>("Time")}
            });

            Details = Add(new Table("Details"));
        }

        public Table Details { get; set; }

        public Table Messages { get; set; }
    }
}