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
        IEnumerable<OutgoingMessage> Read(string queue, Guid lastMessageId, TimeSpan hideTimeout);
        QueueStatistics Statistics(string queue);
        void ConsumeMessage(Guid msgId);
    }
}