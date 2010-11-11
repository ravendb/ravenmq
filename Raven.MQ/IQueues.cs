using System;
using System.Collections.Generic;
using Raven.Abstractions.Commands;
using Raven.Abstractions.Data;
using Raven.Http;

namespace RavenMQ
{
    public interface IQueues : IResourceStore
    {
        Guid Enqueue(IncomingMessage incomingMessage);

        ReadResults Read(ReadRequest readRequest);
        QueueStatistics Statistics(string queue);
        void ConsumeMessage(Guid msgId);
        void ResetMessage(Guid msgId);

        void Batch(params ICommand[] commands);
    }
}