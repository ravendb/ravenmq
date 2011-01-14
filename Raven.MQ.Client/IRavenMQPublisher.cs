using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Raven.Abstractions.Data;

namespace Raven.MQ.Client
{
	public interface IRavenMQPublisher
	{
		IRavenMQPublisher AddRaw(IncomingMessage message);
		IRavenMQPublisher Add(string queue, object msg, JObject metadata);
		IRavenMQPublisher Add(string queue, object msg);

		Task PublishAsync();
	}
}