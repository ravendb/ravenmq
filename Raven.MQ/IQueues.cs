using System;
using System.Collections.Generic;
using Raven.Http;
using RavenMQ.Data;

namespace RavenMQ
{
    public interface IQueues : IResourceStore
    {
        Guid Enqueue(IncomingMessage incomingMessage);
        IEnumerable<OutgoingMessage> Read(string queue, Guid lastMessageId);
        QueueStatistics Statistics(string queue);
    }
}