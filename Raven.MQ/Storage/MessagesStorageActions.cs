using System;
using Newtonsoft.Json.Linq;
using Raven.Abstractions.Data;
using Raven.Munin;
using System.Linq;
using RavenMQ.Impl;

namespace RavenMQ.Storage
{
    public class MessagesStorageActions
    {
        private readonly Table messages;
        private readonly Table pendingMessages;
        private readonly QueuesStorageActions queuesStorageActions;
        private readonly IUuidGenerator uuidGenerator;

        public MessagesStorageActions(Table messages, Table pendingMessages, QueuesStorageActions queuesStorageActions, IUuidGenerator uuidGenerator)
        {
            this.messages = messages;
            this.pendingMessages = pendingMessages;
            this.queuesStorageActions = queuesStorageActions;
            this.uuidGenerator = uuidGenerator;
        }

        public Guid Enqueue(string queue, DateTime expiry, byte[] data)
        {
            var msgId = uuidGenerator.CreateSequentialUuid();
            messages.Put(new JObject
            {
                {"MsgId", msgId.ToByteArray()},
                {"Queue", queue},
                {"Expiry", expiry}
            }, data);
            return msgId;
        }

        public OutgoingMessage Dequeue(string queue, Guid after)
        {
            var key = new JObject
            {
                { "MsgId", after.ToByteArray() }
            };
            var result = messages["ByMsgId"].SkipAfter(key)
                .Where(x => new PathComaparable(x.Value<string>("Queue")).CompareTo(queue) == 0)
                .FirstOrDefault();
            if (result == null)
                return null;

            var readResult = messages.Read(result);
            if (readResult == null)
                return null;

            return new OutgoingMessage
            {
                Id = new Guid(readResult.Key.Value<byte[]>("MsgId")),
                Queue = readResult.Key.Value<string>("Queue"),
                Data = readResult.Data(),
                Expiry = readResult.Key.Value<DateTime>("Expiry")
            };
        }

        public void ConsumeMessage(Guid msgId, Func<DateTime, string, bool> shouldConsumeMessage)
        {
            var key = new JObject { { "MsgId", msgId.ToByteArray() } };
            var readResult = messages.Read(key);
            if (readResult == null)
            {
                var pendingResult = pendingMessages.Read(key);
                if (pendingResult != null)
                {
                    pendingMessages.Remove(key);
                    var pendingQueue = pendingResult.Key.Value<string>("Queue");
                    var pendingExpiry = pendingResult.Key.Value<DateTime>("Expiry");
                    if (shouldConsumeMessage(pendingExpiry, pendingQueue) == false)
                    {
                        ((JObject)pendingResult.Key).Remove("Hide");
                        messages.UpdateKey(pendingResult.Key);
                    }
                    else
                    {
                        queuesStorageActions.DecrementMessageCount(pendingQueue);
                    }
                }
                return;
            }

            var queue = readResult.Key.Value<string>("Queue");
            var epxiry = readResult.Key.Value<DateTime>("Expiry");
            if (shouldConsumeMessage(epxiry, queue) == false)
                return;
            messages.Remove(key);
            queuesStorageActions.DecrementMessageCount(queue);
        }

        public void ResetMessage(Guid msgId)
        {
            var key = new JObject { { "MsgId", msgId.ToByteArray() } };
            ResetMessage(key);
        }

        private void ResetMessage(JToken key)
        {
            var readResult = pendingMessages.Read(key);
            if (readResult == null)
                return;

            var jObject = ((JObject)readResult.Key);
            var jProperty = jObject.Property("Hide");
            if (jProperty != null)
                jProperty.Remove();

        	messages.UpdateKey(jObject);
        }

        public void HideMessageFor(Guid msgId, TimeSpan hideTimeout)
        {
            var key = new JObject { { "MsgId", msgId.ToByteArray() } };
            var readResult = messages.Read(key);
            if (readResult == null)
                return;

            messages.Remove(key);

            readResult.Key["Hide"] = DateTime.UtcNow.Add(hideTimeout);
            pendingMessages.UpdateKey(readResult.Key);
        }

        public void ResetExpiredMessages()
        {
            foreach (var key in pendingMessages["ByHideDesc"]
                .SkipTo(new JObject{{"Hide", DateTime.UtcNow}}))
            {
                ResetMessage(key);
            }
        }
    }
}