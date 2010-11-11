using System.ComponentModel.Composition;
using Newtonsoft.Json.Linq;
using Raven.Abstractions.Data;
using RavenMQ.Impl;

namespace RavenMQ.Plugins
{
    [InheritedExport]
    public abstract class AbstractMessageEnqueuedTrigger
    {
        public IQueues Queues { get; private set; }

        /// <summary>
        /// Initialize the trigger
        /// </summary>
        public void Initialize(IQueues queues)
        {
            Queues = queues;
            Initialize();
        }

        /// <summary>
        /// Subclasses can use this method to initialize themselves
        /// </summary>
        protected virtual void Initialize()
        {
            
        }

        /// <summary>
        /// Decide whatever to veto the message will be enqueued or not.
        /// </summary>
        public virtual MessageVeto VetoMessage(IncomingMessage message)
        {
            return MessageVeto.Allowed;
        }

        /// <summary>
        /// Happen before a message is enqueued. The trigger is free to modify the message.
        /// </summary>
        /// <returns>Potentially modified message data</returns>
        public virtual void BeforeMessageEnqueued(IncomingMessage message)
        {
        }
    }
}