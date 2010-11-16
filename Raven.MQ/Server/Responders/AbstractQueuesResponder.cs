using Raven.Http;
using RavenMQ.Config;
using RavenMQ.Impl;

namespace RavenMQ.Server.Responders
{
    public abstract class AbstractQueuesResponder : AbstractRequestResponder
    {
        public Queues Queues
        {
            get { return (Queues)ResourceStore; }
        }

        public InMemoryRavenConfiguration Configuration
        {
            get { return (InMemoryRavenConfiguration)ResourceStore.Configuration; }
        }
    }
}