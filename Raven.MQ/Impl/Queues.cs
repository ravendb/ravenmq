using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using Raven.Abstractions.Commands;
using Raven.Abstractions.Data;
using Raven.Abstractions.Extensions;
using Raven.Http;
using RavenMQ.Config;
using RavenMQ.Extensions;
using RavenMQ.Plugins;
using RavenMQ.Storage;
using RavenMQ.Subscriptions;

namespace RavenMQ.Impl
{
    public class Queues : IQueues, IUuidGenerator
    {
        private static long _sequentialUuidCounter;
        private readonly InMemoryRavenConfiguration configuration;
        private readonly TransactionalStorage transactionalStorage;
        private long currentEtagBase;

        [ImportMany]
        public IEnumerable<AbstractMessageEnqueuedTrigger> MessageEnqueuedTriggers { get; set; }



        public Queues(InMemoryRavenConfiguration configuration)
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

            Subscriptions = new SubscriptionsSource();

            configuration.Container.SatisfyImportsOnce(this);

            MessageEnqueuedTriggers.Apply(x=>x.Initialize(this));
        }

        #region IQueues Members

        public ISubscriptionsSource Subscriptions { get; private set; }

        public Guid Enqueue(IncomingMessage incomingMessage)
        {
            AssertValidQueuePath(incomingMessage.Queue);
           
            Guid result = Guid.Empty;
            transactionalStorage.Batch(actions =>
            {
                MessageEnqueuedTriggers.Apply(trigger => trigger.BeforeMessageEnqueued(incomingMessage));

                var bytes = incomingMessage.Metadata.ToBytes();
                var ms = new MemoryStream(bytes.Length + incomingMessage.Data.Length);
                ms.Write(bytes, 0, bytes.Length);
                ms.Write(incomingMessage.Data, 0, incomingMessage.Data.Length);


                actions.Queues.IncrementMessageCount(incomingMessage.Queue);
                result = actions.Messages.Enqueue(incomingMessage.Queue, DateTime.UtcNow.Add(incomingMessage.TimeToLive),
                                         ms.ToArray());
            });
            MessageEnqueuedTriggers.Apply(
                enqueuedTrigger => transactionalStorage.ExecuteImmediatelyOrRegisterForSyncronization(
                    enqueuedTrigger, 
                    incomingMessage,
                    msgs => MessageEnqueuedTriggers.Apply(x => x.AfterCommit(msgs.Cast<IncomingMessage>()))
                    ));
            return result;
        }

        private static void AssertValidQueuePath(string queue)
        {
            if (queue.StartsWith("/queues/", StringComparison.InvariantCultureIgnoreCase) ||
                queue.StartsWith("/streams/", StringComparison.InvariantCultureIgnoreCase) ||
                queue.StartsWith("/remotes/", StringComparison.InvariantCultureIgnoreCase))
                return;
            throw new ArgumentException("Queue name does not starts with '/queues/' or '/streams/' or '/remotes/'");
        }

        public void ConsumeMessage(Guid msgId)
        {
            transactionalStorage.Batch(actions=> actions.Messages.ConsumeMessage(msgId, ShouldConsumeMessage));
        }

        public void ResetMessage(Guid msgId)
        {
            transactionalStorage.Batch(actions => actions.Messages.ResetMessage(msgId));
        }

        public void Batch(params ICommand[] commands)
        {
            transactionalStorage.Batch(_ =>
            {
                foreach (var command in commands)
                {
                    switch (command.Type)
                    {
                        case CommandType.Consume:
                            var consumeCmd = (ConsumeCommand) command;
                            ConsumeMessage(consumeCmd.MessageId);
                            break;
                        case CommandType.Enqueue:
                            var enqueueCmd = (EnqueueCommand) command;
                            Enqueue(enqueueCmd.Message);
                            break;
                        case CommandType.Read:
                            var readCmd = (ReadCommand) command;
                            readCmd.Result = Read(readCmd.ReadRequest);
                            break;
                        case CommandType.Reset:
                             var resetCmd = (ResetCommand) command;
                             ResetMessage(resetCmd.MessageId);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            });
        }

        public ReadResults Read(ReadRequest readRequest)
        {
            bool hasMoreItems = false;
            var msgs = new List<OutgoingMessage>();
            var realPageSize = Math.Min(configuration.MaxPageSize, readRequest.PageSize);

            // have to be a separate tranasction, so the next read will get it
            // we expect that most of the time this is a no op, because there won't be any expired messages
            // this also follows the rule of only making the change when it is actually happening, so 
            // until you read, there isn't any activity in the system
            transactionalStorage.Batch(actions => actions.Messages.ResetExpiredMessages());

            transactionalStorage.Batch(actions =>
            {
                var outgoingMessage = actions.Messages.Dequeue(readRequest.Queue, readRequest.LastMessageId);
                while (outgoingMessage != null && msgs.Count < realPageSize)
                {
                    if(ShouldConsumeMessage(outgoingMessage.Expiry,outgoingMessage.Queue))
                    {
                        actions.Messages.HideMessageFor(outgoingMessage.Id, readRequest.HideTimeout);
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
                    outgoingMessage = actions.Messages.Dequeue(readRequest.Queue, outgoingMessage.Id);
                }

                hasMoreItems = outgoingMessage != null;
            });
            return new ReadResults
            {
                HasMoreResults = hasMoreItems,
                Results = msgs,
                Queue = readRequest.Queue
            };
        }

        public QueueStatistics Statistics(string queue)
        {
            AssertValidQueuePath(queue);
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
            var increment = Interlocked.Increment(ref _sequentialUuidCounter);
            var currentAsBytes = BitConverter.GetBytes(increment);
            Array.Reverse(currentAsBytes);
            var bytes = new byte[16];
            Array.Copy(ticksAsBytes, 0, bytes, 0, ticksAsBytes.Length);
            Array.Copy(currentAsBytes, 0, bytes, 8, currentAsBytes.Length);
            return new Guid(bytes);
        }

        #endregion

        private static bool ShouldIncludeMessage(OutgoingMessage msg)
        {
            return msg.Expiry > DateTime.UtcNow;
        }

        private static bool ShouldConsumeMessage(DateTime expiry, string queue)
        {
            if (DateTime.UtcNow > expiry)
                return true;
            return queue.StartsWith("/queues", StringComparison.InvariantCultureIgnoreCase);
        }

        public IRaveHttpnConfiguration Configuration
        {
            get { return configuration; }
        }

        public IEnumerable<string> GetQueueNames(int start, int pageSize)
        {
            var names = new string[0];
            transactionalStorage.Batch(actions => names = actions
                .Queues.GetQueueNames()
                    .Skip(start)
                    .Take(pageSize)
                    .ToArray()
                    );
            return names;
        }


		static string buildVersion;
		public static string BuildVersion
		{
			get
			{
				if (buildVersion == null)
					buildVersion = FileVersionInfo.GetVersionInfo(typeof(Queues).Assembly.Location).FilePrivatePart.ToString();
				return buildVersion;
			}
		}

		static string productVersion;

		public static string ProductVersion
		{
			get
			{
				if (productVersion == null)
					productVersion = FileVersionInfo.GetVersionInfo(typeof(Queues).Assembly.Location).ProductVersion;
				return productVersion;
			}
		}
    }
}