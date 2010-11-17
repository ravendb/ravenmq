using System;
using System.Collections.Generic;
using Raven.Abstractions.Data;

namespace Raven.MQ.Client.Impl
{
    public class RavenMQContext : IRavenMQContext
    {
        public List<IncomingMessage> Messages = new List<IncomingMessage>();

        public void Send(IncomingMessage msg)
        {
            Messages.Add(msg);
        }
    }
}