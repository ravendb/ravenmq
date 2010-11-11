using System;
using RavenMQ.Data;
using Xunit;
using System.Linq;

namespace Raven.MQ.Tests
{
    public class Streams : AbstractQueuesTest
    {
        [Fact]
        public void Can_add_and_get_item_to_stream()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/streams/customers/1234",
                Data = new byte[]{1,2,3},
            });

            Assert.NotNull(queues.Read("/streams/customers/1234", Guid.Empty).First());
        }

        [Fact]
        public void Can_read_item_more_than_once()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/streams/customers/1234",
                Data = new byte[] { 1, 2, 3 },
            });

            var outgoingMessage = queues.Read("/streams/customers/1234", Guid.Empty).First();
            queues.ConsumeMessage(outgoingMessage.Id);
            Assert.NotNull(outgoingMessage);
            queues.ConsumeMessage(outgoingMessage.Id);
            Assert.NotNull(queues.Read("/streams/customers/1234", Guid.Empty).First());
            queues.ConsumeMessage(outgoingMessage.Id);
            Assert.NotNull(queues.Read("/streams/customers/1234", Guid.Empty).First());
        }
    }
}