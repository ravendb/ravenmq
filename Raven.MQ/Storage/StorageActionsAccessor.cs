using System;

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
        }

        public MessagesActions Messages { get; private set; }
    }
}