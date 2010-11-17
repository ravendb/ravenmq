using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Raven.Abstractions.Data;

namespace Raven.MQ.Client.Impl
{
    public class RavenMQContext : IRavenMQContext
    {
        private readonly Func<IEnumerable<IncomingMessage>, Task> flushFunc;
        public List<IncomingMessage> Messages = new List<IncomingMessage>();

        public RavenMQContext(Func<IEnumerable<IncomingMessage>, Task> flushFunc)
        {
            this.flushFunc = flushFunc;
        }

        public void Send(IncomingMessage msg)
        {
            Messages.Add(msg);
        }

        public Task FlushAsync()
        {
            var task = flushFunc(Messages);
            Messages = new List<IncomingMessage>();
            return task;
        }
    }
}