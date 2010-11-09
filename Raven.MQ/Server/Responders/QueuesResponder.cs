using Raven.Http;
using RavenMQ.Config;
using RavenMQ.Impl;

namespace RavenMQ.Server.Responders
{
    public abstract class QueuesResponder : AbstractRequestResponder
    {
        public Queues Queues
        {
            get { return (Queues)ResourceStore; }
        }

        public InMemroyRavenConfiguration Configuration
        {
            get { return (InMemroyRavenConfiguration)ResourceStore.Configuration; }
        }
    }
}