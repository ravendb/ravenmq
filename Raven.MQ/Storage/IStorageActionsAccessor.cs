using System;
using System.Collections;
using System.Collections.Generic;

namespace RavenMQ.Storage
{
    public interface IStorageActionsAccessor
    {
        GeneralStorageActions General { get; }
        MessagesStorageActions Messages { get; }
        QueuesStorageActions Queues { get; }

        IDictionary<object, List<object>> Items { get; }

        event Action OnCommit;
    }
}