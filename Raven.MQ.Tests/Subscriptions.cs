using System;
using Raven.Abstractions.Commands;
using Raven.Abstractions.Data;
using RavenMQ.Subscriptions;
using Xunit;

namespace Raven.MQ.Tests
{
    public class Subscriptions : AbstractQueuesTest
    {
        [Fact]
        public void Can_get_notifications_about_subscription_changes()
        {
            var dummySubscription = new DummySubscription();
            queues.Subscriptions.AddSubscription(dummySubscription);

            queues.Enqueue(new IncomingMessage
            {
                Queue = "/queues/mailboxes/1234",
                Data = new byte[] {1, 2, 3, 4}
            });

            Assert.Equal(1, dummySubscription.Notified);
        }

        [Fact]
        public void Can_get_notifications_about_subscription_changes_in_single_batch()
        {
            var dummySubscription = new DummySubscription();
            queues.Subscriptions.AddSubscription(dummySubscription);

            queues.Batch(
                new EnqueueCommand
                {
                    Message = new IncomingMessage
                    {
                        Queue = "/queues/mailboxes/1234",
                        Data = new byte[] {1, 2, 3, 4}
                    }
                },
                new EnqueueCommand
                {
                    Message = new IncomingMessage
                    {
                        Queue = "/queues/mailboxes/1234",
                        Data = new byte[] {1, 2, 3, 4}
                    }
                } 
                );

            Assert.Equal(1, dummySubscription.Notified);
        }

        public class DummySubscription : ISubscription
        {
            public bool IsMatch(SubscriptionFilterInfo subscriptionFilterInfo)
            {
                return subscriptionFilterInfo.Queue == "/queues/mailboxes/1234";
            }

            public int Notified { get; set; }

            public void NotifyChanges()
            {
                Notified ++;
            }
        }
    }
}