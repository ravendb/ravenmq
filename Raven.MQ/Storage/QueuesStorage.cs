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
                {"ByMsgId", key => new ComparableByteArray(key.Value<byte[]>("MsgId"))}
            });

            Identity = Add(new Table(x => x.Value<string>("name"), "Identity"));
            
            Details = Add(new Table("Details"));
        }

        public Table Identity { get; set; }

        public Table Details { get; set; }

        public Table Messages { get; set; }
    }
}