using System;
using System.Threading.Tasks;
using Raven.Abstractions.Data;

namespace Raven.MQ.Client
{
	public interface IRavenMQConnection : IDisposable
	{
		Task ConnectToServerTask { get; }
		IDisposable SubscribeEnvelope<TMsg>(string queue, Action<IRavenMQContext, Envelope<TMsg>> action);
		IDisposable Subscribe<TMsg>(string queue, Action<IRavenMQContext, TMsg> action);
		IDisposable SubscribeRaw(string queue, Action<IRavenMQContext, OutgoingMessage> action);

		IRavenMQPublisher StartPublishing { get; }
	}
}