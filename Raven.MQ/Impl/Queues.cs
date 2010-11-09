using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using RavenMQ.Config;
using RavenMQ.Data;
using RavenMQ.Extensions;
using RavenMQ.Storage;

namespace RavenMQ.Impl
{
    public class Queues : IQueues, IUuidGenerator
    {
        private static long sequentialUuidCounter;
        private readonly InMemroyRavenConfiguration configuration;
        private readonly TransactionalStorage transactionalStorage;
        private long currentEtagBase;

        public Queues(InMemroyRavenConfiguration configuration)
        {
            this.configuration = configuration;
            transactionalStorage = new TransactionalStorage(configuration);
            try
            {
                transactionalStorage.Initialize(this);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
            transactionalStorage.Batch(actions => currentEtagBase = actions.General.GetNextIdentityValue("Raven/Etag"));
        }

        #region IQueues Members

        public void Enqueue(IncomingMessage incomingMessage)
        {
            var bytes = incomingMessage.Metadata.ToBytes();
            var ms = new MemoryStream(bytes.Length + incomingMessage.Data.Length);
            ms.Write(bytes, 0, bytes.Length);
            ms.Write(incomingMessage.Data, 0, incomingMessage.Data.Length);

            transactionalStorage.Batch(actions =>
            {
                actions.Queues.IncrementMessageCount(incomingMessage.Queue);
                actions.Messages.Enqueue(incomingMessage.Queue, DateTime.UtcNow.Add(incomingMessage.TimeToLive),
                                         ms.ToArray());
            });
        }

        public IEnumerable<OutgoingMessage> Read(string queue, Guid lastMessageId)
        {
            var msgs = new List<OutgoingMessage>();
            transactionalStorage.Batch(actions =>
            {
                var outgoingMessage = actions.Messages.Dequeue(queue, lastMessageId);
                while (outgoingMessage != null && msgs.Count < configuration.MaxPageSize)
                {
                    if (ShouldConsumeMessage(outgoingMessage))
                    {
                        actions.Queues.DecrementMessageCount(outgoingMessage.Queue);
                        actions.Messages.ConsumeMessage(outgoingMessage.Id);
                    }
                    if (ShouldIncludeMessage(outgoingMessage))
                    {
                        var buffer = outgoingMessage.Data;
                        var memoryStream = new MemoryStream(buffer);
                        outgoingMessage.Metadata = memoryStream.ToJObject();
                        outgoingMessage.Data = new byte[outgoingMessage.Data.Length - memoryStream.Position];
                        Array.Copy(buffer, memoryStream.Position, outgoingMessage.Data, 0, outgoingMessage.Data.Length);
                        msgs.Add(outgoingMessage);
                    }
                    outgoingMessage = actions.Messages.Dequeue(queue, outgoingMessage.Id);
                }
            });
            return msgs;
        }

        public QueueStatistics Statistics(string queue)
        {
            QueueStatistics result = null;
            transactionalStorage.Batch(actions => result = actions.Queues.Statistics(queue));
            if (result == null)// non existant queue is also empty queue by default
            {
                return new QueueStatistics
                {
                    Name = queue,
                    NumberOfMessages = 0
                };
            }

            return result;
        }

        public void Dispose()
        {
            if (transactionalStorage != null)
                transactionalStorage.Dispose();
        }

        #endregion

        #region IUuidGenerator Members

        public Guid CreateSequentialUuid()
        {
            var ticksAsBytes = BitConverter.GetBytes(currentEtagBase);
            Array.Reverse(ticksAsBytes);
            var increment = Interlocked.Increment(ref sequentialUuidCounter);
            var currentAsBytes = BitConverter.GetBytes(increment);
            Array.Reverse(currentAsBytes);
            var bytes = new byte[16];
            Array.Copy(ticksAsBytes, 0, bytes, 0, ticksAsBytes.Length);
            Array.Copy(currentAsBytes, 0, bytes, 8, currentAsBytes.Length);
            return new Guid(bytes);
        }

        #endregion

        private bool ShouldIncludeMessage(OutgoingMessage msg)
        {
            return msg.Expiry > DateTime.UtcNow;
        }

        private bool ShouldConsumeMessage(OutgoingMessage msg)
        {
            if (DateTime.UtcNow > msg.Expiry)
                return true;
            return msg.Queue.StartsWith("/queues", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}