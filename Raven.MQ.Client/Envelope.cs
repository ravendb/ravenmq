using System;
using Newtonsoft.Json.Linq;

namespace Raven.MQ.Client
{
	public class Envelope<T>
	{
		public T Message { get; set; }
		public Guid Id { get; set; }
		public JObject Metadata { get; set; }
		public DateTime Expiry { get; set; }
		public string Queue { get; set; }
	}
}