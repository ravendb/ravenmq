using System;
using Newtonsoft.Json.Linq;
using Raven.Abstractions.Commands;
using Raven.Abstractions.Data;
using Xunit;
using System.Linq;

namespace Raven.MQ.Tests
{
    public class BatchOperations : AbstractQueuesTest
    {
        [Fact]
        public void Can_save_two_messages_in_one_shot()
        {
            queues.Batch(
                new EnqueueCommand
                {
                    Message = new IncomingMessage
                    {
                        Data = new byte[] {1, 2, 3},
                        Metadata = new JObject(),
                        Queue = "/queues/abc",
                    }
                },
                new EnqueueCommand
                {
                    Message = new IncomingMessage
                    {
                        Data = new byte[] {1, 2, 3},
                        Metadata = new JObject(),
                        Queue = "/queues/cba",
                    }
                });

            Assert.NotNull(queues.Read(new ReadRequest
            {
                LastMessageId = Guid.Empty,
                Queue = "/queues/abc"
            }).Results.FirstOrDefault());

            Assert.NotNull(queues.Read(new ReadRequest
            {
                LastMessageId = Guid.Empty,
                Queue = "/queues/cba"
                
            }).Results.FirstOrDefault());
        }

        [Fact]
        public void Can_read_from_two_queues_in_one_shot()
        {
            queues.Batch(
                new EnqueueCommand
                {
                    Message = new IncomingMessage
                    {
                        Data = new byte[] { 1, 2, 3 },
                        Metadata = new JObject(),
                        Queue = "/queues/abc",
                    }
                },
                new EnqueueCommand
                {
                    Message = new IncomingMessage
                    {
                        Data = new byte[] { 1, 2, 3 },
                        Metadata = new JObject(),
                        Queue = "/queues/cba",
                    }
                });

            var cbaQ = new ReadCommand
            {
                ReadRequest =
                    {
                        LastMessageId = Guid.Empty,
                        Queue = "/queues/cba",
                    }
            };
            var abcQ = new ReadCommand
            {
                ReadRequest =
                {
                    Queue = "/queues/abc",
                    LastMessageId = Guid.Empty
                }
            };
            queues.Batch(abcQ,cbaQ);

            Assert.NotEmpty(abcQ.Result.Results);
            Assert.NotEmpty(cbaQ.Result.Results);
        }
    }
}