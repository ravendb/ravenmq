using System;
using System.Collections.Generic;
using RavenMQ.Data;

namespace RavenMQ
{
    public interface IQueues : IDisposable
    {
        void Enqueue(IncomingMessage incomingMessage);
        IEnumerable<OutgoingMessage> Read(string queue, Guid lastMessageId);
    }
}