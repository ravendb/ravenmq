using System;
using System.Collections.Generic;
using Raven.Abstractions.Commands;
using Raven.Abstractions.Data;
using Raven.Http;
using RavenMQ.Subscriptions;

namespace RavenMQ
{
    public interface IQueues : IResourceStore
    {
        ISubscriptionsSource Subscriptions { get; }

        Guid Enqueue(IncomingMessage incomingMessage);

        ReadResults Read(ReadRequest readRequest);
        
        QueueStatistics Statistics(string queue);
        
        void ConsumeMessage(Guid msgId);

        void ResetMessage(Guid msgId);

        void Batch(params ICommand[] commands);


    }
}