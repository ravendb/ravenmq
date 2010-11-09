using System;
using RavenMQ.Impl;

namespace RavenMQ.Storage
{
    public class StorageActionsAccessor : IStorageActionsAccessor
    {
        private readonly QueuesStorage queuesStroage;
        private readonly IUuidGenerator uuidGenerator;

        public StorageActionsAccessor(QueuesStorage queuesStroage, IUuidGenerator uuidGenerator)
        {
            this.queuesStroage = queuesStroage;
            this.uuidGenerator = uuidGenerator;
            Messages = new MessagesActions(queuesStroage.Messages, uuidGenerator);
            General = new GeneralStorageActions(queuesStroage.Identity);
        }

        public GeneralStorageActions General { get; set; }

        public MessagesActions Messages { get; private set; }
    }
}