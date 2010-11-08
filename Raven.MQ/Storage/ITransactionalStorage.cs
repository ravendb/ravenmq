using System;

namespace RavenMQ.Storage
{
    public interface ITransactionalStorage : IDisposable
    {
        void Batch(Action<IStorageActionsAccessor> action);
        bool Initialize(IUuidGenerator generator);
    }
}