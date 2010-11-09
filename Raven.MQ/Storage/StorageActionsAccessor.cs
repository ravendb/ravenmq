using System;
using RavenMQ.Impl;

namespace RavenMQ.Storage
{
    public class StorageActionsAccessor : IStorageActionsAccessor
    {
        public StorageActionsAccessor(QueuesStorage queuesStroage, IUuidGenerator uuidGenerator)
        {
            Messages = new MessagesStorageActions(queuesStroage.Messages, uuidGenerator);
            General = new GeneralStorageActions(queuesStroage.Identity);
            Queues = new QueuesStorageActions(queuesStroage.Queues);
        }

        public QueuesStorageActions Queues { get; set; }

        public GeneralStorageActions General { get; set; }

        public MessagesStorageActions Messages { get; set; }
    }
}