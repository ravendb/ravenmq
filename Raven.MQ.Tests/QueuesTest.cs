using System;
using System.Linq;
using Raven.Abstractions.Data;
using Xunit;

namespace Raven.MQ.Tests
{
    public class QueuesTest : AbstractQueuesTest
    {
        [Fact]
        public void After_reading_msg_will_consume_it_and_not_read_it_again()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] {1, 2, 3, 4}
            });

            var outgoingMessage = queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes/1234",
            }).Results.First();
            queues.ConsumeMessage(outgoingMessage.Id);
            Assert.Null(queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes/1234",
            }).Results.FirstOrDefault());
            Assert.Equal(0, queues.Statistics("/queues/mailboxes/1234").NumberOfMessages);
        }

        [Fact]
        public void After_reading_msg_can_reset_it_and_read_it_again()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] { 1, 2, 3, 4 }
            });

            var outgoingMessage = queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes/1234",
            }).Results.First();
            queues.ResetMessage(outgoingMessage.Id);
            Assert.NotNull(queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes/1234",
            }).Results.FirstOrDefault());
        }

        [Fact]
        public void After_reading_message_will_not_get_it_for_specified_timeout()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] { 1, 2, 3, 4 }
            });


			Assert.NotNull(queues.Read(new ReadRequest
			{
				Queue = "/queues/mailboxes/1234",
			}));
            Assert.Null(queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes/1234",
            }).Results.FirstOrDefault());
            Assert.Equal(1, queues.Statistics("/queues/mailboxes/1234").NumberOfMessages);
        }

        [Fact]
        public void After_timeout_passed_message_will_be_available_again()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] { 1, 2, 3, 4 }
            });

            queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes/1234",
                HideTimeout = TimeSpan.FromSeconds(-1)
            }).Results.First();
            Assert.NotNull(queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes/1234",
            }).Results.FirstOrDefault());
            Assert.Equal(1, queues.Statistics("/queues/mailboxes/1234").NumberOfMessages);
        }

        [Fact]
        public void Will_not_read_message_after_its_ttl_expired()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] {1, 2, 3, 4},
                TimeToLive = TimeSpan.FromMinutes(-1)
            });

            Assert.Null(queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes/1234",
            }).Results.FirstOrDefault());
        }

        [Fact]
        public void Can_read_queues_stats()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] { 1, 2, 3, 4 },
                TimeToLive = TimeSpan.FromMinutes(-1)
            });

            var stats = queues.Statistics("/queues/mailboxes/1234");
            Assert.Equal(1, stats.NumberOfMessages);
        }

        [Fact]
        public void Queue_names_are_case_insensitive_for_stats()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] { 1, 2, 3, 4 },
                TimeToLive = TimeSpan.FromMinutes(-1)
            });

            var stats = queues.Statistics("/queues/MAILBOXES/1234");
            Assert.Equal(1, stats.NumberOfMessages);
        }

        [Fact]
        public void Queue_names_are_case_insensitive_for_reading_messages()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] { 1, 2, 3, 4 },
                TimeToLive = TimeSpan.FromMinutes(-1)
            });

            Assert.Null(queues.Read(new ReadRequest
            {
                Queue = "/queues/MAILBOXES/1234",
            }).Results.FirstOrDefault());
        }

        [Fact]
        public void Can_get_aggregate_information_about_queues()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] { 1, 2, 3, 4 },
                TimeToLive = TimeSpan.FromMinutes(-1)
            });

            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/4321",
                Data = new byte[] { 1, 2, 3, 4 },
                TimeToLive = TimeSpan.FromMinutes(-1)
            });

            var stats = queues.Statistics("/queues/MAILBOXES");
            Assert.Equal(2, stats.NumberOfMessages);
      
        }

        [Fact]
        public void Can_consume_message_from_child_parent()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] { 1, 2, 3, 4 },
            });

            Assert.NotNull(queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes",
            }).Results.FirstOrDefault());
        }

        [Fact]
        public void Will_not_get_info_from_separate_queue()
        {
            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] { 1, 2, 3, 4 },
            });

            Assert.Null(queues.Read(new ReadRequest
            {
                Queue = "/queues/mailboxes/4321",
            }).Results.FirstOrDefault());
        }
    }
}