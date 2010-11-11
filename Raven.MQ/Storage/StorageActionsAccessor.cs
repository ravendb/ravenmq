using System;
using RavenMQ.Impl;

namespace RavenMQ.Storage
{
    public class StorageActionsAccessor : IStorageActionsAccessor
    {
        public StorageActionsAccessor(QueuesStorage queuesStroage, IUuidGenerator uuidGenerator)
        {
            Queues = new QueuesStorageActions(queuesStroage.Queues);
            Messages = new MessagesStorageActions(queuesStroage.Messages, Queues, uuidGenerator);
            General = new GeneralStorageActions(queuesStroage.Identity);
        }

        public QueuesStorageActions Queues { get; set; }

        public GeneralStorageActions General { get; set; }

        public MessagesStorageActions Messages { get; set; }
    }
}