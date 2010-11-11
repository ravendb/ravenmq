using System;
using System.Collections.Generic;
using RavenMQ.Impl;

namespace RavenMQ.Storage
{
    public class StorageActionsAccessor : IStorageActionsAccessor
    {
        public StorageActionsAccessor(QueuesStorage queuesStroage, IUuidGenerator uuidGenerator)
        {
            Items = new Dictionary<object, List<object>>();
            Queues = new QueuesStorageActions(queuesStroage.Queues);
            Messages = new MessagesStorageActions(queuesStroage.Messages, Queues, uuidGenerator);
            General = new GeneralStorageActions(queuesStroage.Identity);
        }

        public QueuesStorageActions Queues { get; set; }

        public IDictionary<object, List<object>> Items { get; private set; }

        public event Action OnCommit;

        public GeneralStorageActions General { get; set; }

        public MessagesStorageActions Messages { get; set; }

        public void InvokeOnCommit()
        {
            var handler = OnCommit;
            if (handler != null) 
                handler();
        }
    }
}