using System;
using System.Linq;
using Raven.Abstractions.Data;
using Xunit;

namespace Raven.MQ.Tests
{
    public class SimpleMessages : AbstractQueuesTest
    {
        [Fact]
        public void Can_send_and_recieve_message_to_queue()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] {1, 2, 3, 4}
            });

            var msg = queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes/1234",
            }).Results.First();

            Assert.NotNull(msg);
            Assert.Equal("/queues/mailboxes/1234", msg.Queue);
            Assert.Equal(new byte[] {1, 2, 3, 4}, msg.Data);
        }

        [Fact]
        public void When_reading_empty_queue_will_return_empty_set()
        {
            var count = queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes/1234",
            }).Results.Count();

            Assert.Equal(0, count);
        }

        [Fact]
        public void Can_send_and_recieve_message_to_queue_with_metadata()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Metadata = {{"important", true}},
                Data = new byte[] {1, 2, 3, 4}
            });

            var msg = queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes/1234",
            }).Results.First();

            Assert.NotNull(msg);
            Assert.Equal("/queues/mailboxes/1234", msg.Queue);
            Assert.Equal(true, msg.Metadata.Value<bool>("important"));
        }
    }
}