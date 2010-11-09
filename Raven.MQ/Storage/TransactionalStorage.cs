using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Raven.Munin;
using RavenMQ.Config;
using RavenMQ.Impl;

namespace RavenMQ.Storage
{
    public class TransactionalStorage : ITransactionalStorage
    {
        private readonly ThreadLocal<IStorageActionsAccessor> current = new ThreadLocal<IStorageActionsAccessor>();
        private readonly InMemroyRavenConfiguration configuration;
        private Timer idleTimer;
        private readonly ReaderWriterLockSlim disposerLock = new ReaderWriterLockSlim();
        private long lastUsageTime;
        private bool disposed;
        private IUuidGenerator uuidGenerator;
        private IPersistentSource persistenceSource;
        private QueuesStorage queuesStroage;

        public TransactionalStorage(InMemroyRavenConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void Dispose()
        {
            disposerLock.EnterWriteLock();
            try
            {
                if (disposed)
                    return;
                if (idleTimer != null)
                    idleTimer.Dispose();
                if (persistenceSource != null)
                    persistenceSource.Dispose();
            }
            finally
            {
                disposed = true;
                disposerLock.ExitWriteLock();
            }
        }

        public void Batch(Action<IStorageActionsAccessor> action)
        {
            if (disposed)
            {
                Trace.WriteLine("TransactionalStorage.Batch was called after it was disposed, call was ignored.");
                return; // this may happen if someone is calling us from the finalizer thread, so we can't even throw on that
            }
            if (current.Value != null)
            {
                action(current.Value);
                return;
            }
            disposerLock.EnterReadLock();
            try
            {
                Interlocked.Exchange(ref lastUsageTime, DateTime.Now.ToBinary());
                using (queuesStroage.BeginTransaction())
                {
                    current.Value = new StorageActionsAccessor(queuesStroage, uuidGenerator);
                    action(current.Value);
                    queuesStroage.Commit();
                }
            }
            finally
            {
                disposerLock.ExitReadLock();
                current.Value = null;
            }
        }

        public bool Initialize(IUuidGenerator generator)
        {
            uuidGenerator = generator;
            if (configuration.RunInMemory == false && Directory.Exists(configuration.DataDirectory) == false)
                Directory.CreateDirectory(configuration.DataDirectory);

            persistenceSource = configuration.RunInMemory
                          ? (IPersistentSource)new MemoryPersistentSource()
                          : new FileBasedPersistentSource(configuration.DataDirectory, "Raven", configuration.TransactionMode == TransactionMode.Safe);

            queuesStroage = new QueuesStorage(persistenceSource);

            idleTimer = new Timer(MaybeOnIdle, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            queuesStroage.Initialze();

            if (persistenceSource.CreatedNew)
            {
                Id = Guid.NewGuid();
                Batch(accessor => queuesStroage.Details.Put("id", Id.ToByteArray()));
            }
            else
            {
                var readResult = queuesStroage.Details.Read("id");
                Id = new Guid(readResult.Data());
            }

            return persistenceSource.CreatedNew;
        }

        private void MaybeOnIdle(object _)
        {
            var ticks = Interlocked.Read(ref lastUsageTime);
            var lastUsage = DateTime.FromBinary(ticks);
            if ((DateTime.Now - lastUsage).TotalSeconds < 30)
                return;

            queuesStroage.PerformIdleTasks();
        }

        public Guid Id { get; private set; }
    }
}