using System;
using RavenMQ;
using RavenMQ.Config;
using RavenMQ.Impl;

namespace Raven.MQ.Tests
{
    public class AbstractQueuesTest : IDisposable
    {
        protected IQueues queues;

        public AbstractQueuesTest()
        {
            queues = new Queues(new InMemroyRavenConfiguration
            {
                RunInMemory = true
            });
        }

        #region IDisposable Members

        public void Dispose()
        {
            queues.Dispose();
        }

        #endregion
    }
}