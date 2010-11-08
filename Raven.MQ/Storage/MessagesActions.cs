using System;
using Newtonsoft.Json.Linq;
using Raven.Munin;

namespace RavenMQ.Storage
{
    public class MessagesActions
    {
        private readonly Table messages;
        private readonly IUuidGenerator uuidGenerator;

        public MessagesActions(Table messages, IUuidGenerator uuidGenerator)
        {
            this.messages = messages;
            this.uuidGenerator = uuidGenerator;
        }

        public void Enqueue(string queue, byte[]data)
        {
            messages.Put(new JObject
            {
                {"MsgId", uuidGenerator.CreateSequentialUuid().ToByteArray()},
                {"QueueName", queue},
                {"Time", DateTime.UtcNow}
            }, data);
        }
    }
}