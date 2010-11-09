using System;
using Newtonsoft.Json.Linq;
using Raven.Munin;
using System.Linq;
using RavenMQ.Data;
using RavenMQ.Impl;

namespace RavenMQ.Storage
{
    public class MessagesStorageActions
    {
        private readonly Table messages;
        private readonly IUuidGenerator uuidGenerator;

        public MessagesStorageActions(Table messages, IUuidGenerator uuidGenerator)
        {
            this.messages = messages;
            this.uuidGenerator = uuidGenerator;
        }

        public void Enqueue(string queue, DateTime expiry, byte[] data)
        {
            messages.Put(new JObject
            {
                {"MsgId", uuidGenerator.CreateSequentialUuid().ToByteArray()},
                {"Queue", queue},
                {"Expiry", expiry}
            }, data);
        }

        public OutgoingMessage Dequeue(string queue, Guid after)
        {
            var key = new JObject
            {
                { "Queue", queue },
                { "MsgId", after.ToByteArray() }
            };
            var result = messages["ByQueueNameAndMsgId"].SkipAfter(key).FirstOrDefault();
            if (result == null)
                return null;

            var readResult = messages.Read(result);

            return new OutgoingMessage
            {
                Id = new Guid(readResult.Key.Value<byte[]>("MsgId")),
                Queue = readResult.Key.Value<string>("Queue"),
                Data = readResult.Data(),
                Expiry = readResult.Key.Value<DateTime>("Expiry")
            };
        }

        public void ConsumeMessage(Guid msgId)
        {
            messages.Remove(new JObject { { "MsgId", msgId.ToByteArray() } });
        }
    }
}