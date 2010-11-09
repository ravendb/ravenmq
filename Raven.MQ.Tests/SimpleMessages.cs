using System;
using RavenMQ;
using RavenMQ.Config;
using RavenMQ.Impl;
using Xunit;
using System.Linq;

namespace Raven.MQ.Tests
{
    public class SimpleMessages : IDisposable
    {
        private IQueues queues;

        public SimpleMessages()
        {
            queues = new Queues(new InMemroyRavenConfiguration
            {
                RunInMemory = true
            });
        }


        [Fact]
        public void Can_send_and_recieve_message_to_queue()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] { 1, 2, 3, 4 }
            });

            var msg = queues.Read("/queues/mailboxes/1234", Guid.Empty).First();

            Assert.NotNull(msg);
            Assert.Equal("/queues/mailboxes/1234", msg.Queue);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, msg.Data);
        }

        [Fact]
        public void Can_send_and_recieve_message_to_queue_with_metadata()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Metadata = {{ "important", true }},
                Data = new byte[] { 1, 2, 3, 4 }
            });

            var msg = queues.Read("/queues/mailboxes/1234", Guid.Empty).First();

            Assert.NotNull(msg);
            Assert.Equal("/queues/mailboxes/1234", msg.Queue);
            Assert.Equal(true, msg.Metadata.Value<bool>("important"));
        }

        public void Dispose()
        {
            queues.Dispose();
        }
    }
}