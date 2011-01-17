using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using Raven.Abstractions.Data;

namespace Raven.MQ.Client
{
	public class RavenMQPublisher : IRavenMQPublisher
	{
		private readonly RavenMQConnection connection;

		private readonly List<IncomingMessage> msgs = new List<IncomingMessage>();

		public RavenMQPublisher(RavenMQConnection connection)
		{
			this.connection = connection;
		}

		public IRavenMQPublisher AddRaw(IncomingMessage message)
		{
			msgs.Add(message);
			return this;
		}

		public IRavenMQPublisher Add(string queue, object msg, JObject metadata)
		{
			var memoryStream = new MemoryStream();
			using(var bsonWriter = new BsonWriter(memoryStream))
				ToJson(msg).WriteTo(bsonWriter);
			return AddRaw(new IncomingMessage
			{
				Data = memoryStream.ToArray(),
				Metadata = metadata,
				Queue = queue,
			});
		}

		private static JToken ToJson(object msg)
		{
			if (msg is JObject)
				return (JToken)msg;
			if(msg is Guid)
				return new JObject
				{
					{"Val", ((Guid)msg).ToByteArray()}
				};
			if (msg is ValueType || msg is string)
				return new JObject
				{
					{"Val", new JValue(msg)}
				};
			return JObject.FromObject(msg);
		}

		public IRavenMQPublisher Add(string queue, object msg)
		{
			return Add(queue, msg, new JObject());
		}

		public Task PublishAsync()
		{
			return connection.PublishMessagesAsync(msgs);
		}
	}
}