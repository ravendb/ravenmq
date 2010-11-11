using System;

namespace RavenMQ.Storage
{
    public interface IStorageActionsAccessor
    {
        GeneralStorageActions General { get; }
        MessagesStorageActions Messages { get; }
        QueuesStorageActions Queues { get; }

        event Action OnCommit;
    }
}