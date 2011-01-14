using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Raven.Abstractions.Data;

namespace Raven.MQ.Client
{
	public interface IRavenMQConnection : IDisposable
	{
		Task ConnectToServerTask { get; }
		IDisposable Subscribe(string queue, Action<IRavenMQContext, OutgoingMessage> action);
		Task PublishAsync(IncomingMessage msg);
		Task PublishMessagesAsync(IEnumerable<IncomingMessage> msgs);
	}
}