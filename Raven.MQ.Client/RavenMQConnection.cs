using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using Raven.Abstractions;
using Raven.Abstractions.Commands;
using Raven.Abstractions.Data;
using Raven.Abstractions.Json;
using Raven.MQ.Client.Impl;
using Raven.MQ.Client.Network;
using Raven.Abstractions.Extensions;
using System.Linq;

namespace Raven.MQ.Client
{
	public class RavenMQConnection : IClientIntegration, IRavenMQConnection
	{
		private readonly Uri queueEndpointSubscriptionPort;
		private readonly Uri queueBulkEndpoint;
		private ClientConnection clientConnection;

		private readonly ConcurrentDictionary<string, Guid> lastEtagPerQeueue =
			new ConcurrentDictionary<string, Guid>(StringComparer.InvariantCultureIgnoreCase);

		private readonly ConcurrentDictionary<string, Action<IRavenMQContext, OutgoingMessage>> actionsPerQueue =
			new ConcurrentDictionary<string, Action<IRavenMQContext, OutgoingMessage>>(StringComparer.InvariantCultureIgnoreCase);

		private Task connectToServerTask;
		private bool disposed;

		public static IRavenMQConnection Connect(string queueEndpoint)
		{
			return new RavenMQConnection(new Uri(queueEndpoint));
		}

		public RavenMQConnection(Uri queueEndpoint)
		{
			var absolutePath = queueEndpoint.AbsolutePath;
			if (absolutePath.EndsWith("/"))
				absolutePath = absolutePath.Substring(0, absolutePath.Length - 1);
			queueEndpointSubscriptionPort = new UriBuilder(queueEndpoint)
			{
				Path = absolutePath + "/subscriptionsPort"
			}.Uri;
			queueBulkEndpoint = new UriBuilder(queueEndpoint)
			{
				Path = absolutePath + "/bulk"
			}.Uri;
			TryReconnecting();
		}

		public Task ConnectToServerTask
		{
			get { return connectToServerTask; }
		}

		public IDisposable SubscribeEnvelope<TMsg>(string queue, Action<IRavenMQContext, Envelope<TMsg>> action)
		{
			return SubscribeRaw(queue, (context, message) =>
			{
				using(var bsonReader = new BsonReader(new MemoryStream(message.Data)))
				{
					var msg = ToMessage<TMsg>(bsonReader);
					action(context, new Envelope<TMsg>
					{
						Expiry = message.Expiry,
						Message = msg,
						Id = message.Id,
						Metadata = message.Metadata,
						Queue = message.Queue
					});
				}
			});
		}

		private static TMsg ToMessage<TMsg>(BsonReader bsonReader)
		{
			if (typeof(Guid) == typeof(TMsg))
			{
				return (TMsg)(object)new Guid(JToken.ReadFrom(bsonReader).Value<byte[]>("Val"));
			}
			if (typeof(TMsg) == typeof(string) || typeof(TMsg).IsValueType)
			{
				return JToken.ReadFrom(bsonReader).Value<TMsg>("Val");
			}
			return new JsonSerializer().Deserialize<TMsg>(bsonReader);
		}

		public IDisposable Subscribe<TMsg>(string queue, Action<IRavenMQContext, TMsg> action)
		{
			return SubscribeEnvelope<TMsg>(queue, (context, env) => action(context, env.Message));
		}

		public IDisposable SubscribeRaw(string queue, Action<IRavenMQContext, OutgoingMessage> action)
		{
			lastEtagPerQeueue.TryAdd(queue, Guid.Empty);
			actionsPerQueue.AddOrUpdate(queue, action, (s, existing) => existing + action);
			SendSubscribtionMessageAsync(queue, ChangeSubscriptionType.Add);
			return new DisposableAction(() =>
			{
				while (true)
				{
					Action<IRavenMQContext, OutgoingMessage> value;
					if (actionsPerQueue.TryGetValue(queue, out value))
						break;

					var withoutCurrentAction = value - action;
					if (withoutCurrentAction == null)
					{
						actionsPerQueue.TryRemove(queue, out value);
						Guid _;
						lastEtagPerQeueue.TryRemove(queue, out _);
						SendSubscribtionMessageAsync(queue, ChangeSubscriptionType.Remove);
						break;
					}

					if (actionsPerQueue.TryUpdate(queue, withoutCurrentAction, value))
					{
						break;
					}
				}
			});
		}

		public IRavenMQPublisher StartPublishing
		{
			get { return new RavenMQPublisher(this); }
		}

		private void SendSubscribtionMessageAsync(string queue, ChangeSubscriptionType type)
		{
			connectToServerTask.ContinueWith(task =>
			{
				if (task.Exception != null)
					return task;

				return clientConnection.Send(JObject.FromObject(new ChangeSubscriptionMessage
				{
					Type = type,
					Queues = { queue }
				}));
			})
			.Unwrap()
			.IgnoreExceptions();
		}

		public void Dispose()
		{
			disposed = true;
			if (clientConnection != null)
				clientConnection.Dispose();
		}

		public void Init(ClientConnection connection)
		{
		}

		public void TryReconnecting()
		{
			if (disposed)
				return;

			var webRequest = WebRequest.Create(queueEndpointSubscriptionPort);
			connectToServerTask = Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse, webRequest.EndGetResponse, null)
				.ContinueWith(getPortResponseTask =>
				{
					using (var response = getPortResponseTask.Result)
					using (var stream = response.GetResponseStream())
					using (var reader = new StreamReader(stream))
					{
						var result = JObject.Load(new JsonTextReader(reader));
						return result.Value<int>("SubscriptionPort");
					}
				})
				.ContinueWith(getPortTask =>
				{
					var result = getPortTask.Result;

					var hostAddresses = Dns.GetHostAddresses(queueEndpointSubscriptionPort.DnsSafeHost);

					return new IPEndPoint(hostAddresses.First(x => x.AddressFamily == AddressFamily.InterNetwork), result);
				})
				.ContinueWith(subscriptionEndpointTask =>
				{
					clientConnection = new ClientConnection(subscriptionEndpointTask.Result, this);

					return clientConnection.Connect();
				})
				.Unwrap()
				.ContinueWith(task =>
				{
					if (task.Exception != null)
						return task;

					return clientConnection.Send(JObject.FromObject(new ChangeSubscriptionMessage
					{
						Type = ChangeSubscriptionType.Set,
						Queues = new List<string>(lastEtagPerQeueue.Keys)
					}));
				})
				.Unwrap()
				.ContinueWith(task =>
				{
					if (task.Exception != null) // retrying if we got an error
					{
						if (disposed)
							return task;

						Console.WriteLine("Failure {0}", DateTime.Now);
						GC.Collect(2);
						GC.WaitForPendingFinalizers();
						
						TaskEx.Delay(TimeSpan.FromSeconds(3))
							.ContinueWith(_ => TryReconnecting());
						return task;
					}
					Console.WriteLine("Success");
					// ask for latest
					return UpdateAsync();
				})
				.Unwrap()
				.IgnoreExceptions();
		}

		public void OnMessageArrived(JObject msg)
		{
			if (msg.Value<bool>("Changed") == false)
				return;
			UpdateAsync();
		}

		public Task UpdateAsync()
		{
			var webRequest = (HttpWebRequest)WebRequest.Create(queueBulkEndpoint);
			webRequest.Method = "POST";
			webRequest.ContentType = "application/bson";
			webRequest.Accept = "application/bson";
			return Task.Factory.FromAsync<Stream>(webRequest.BeginGetRequestStream, webRequest.EndGetRequestStream, null)
					.ContinueWith(getReqStreamTask =>
					{
						getReqStreamTask.AssertNotExceptional();

						var enumerable = lastEtagPerQeueue.Select(x => new ReadCommand
						{
							ReadRequest = { Queue = x.Key, LastMessageId = x.Value, }
						}).Select(command => JObject.FromObject(command, CreateJsonSerializer()));
						return getReqStreamTask.Result.Write(new JArray(enumerable));
					}).Unwrap()
					.ContinueWith(writeRequestTask =>
					{
						writeRequestTask.AssertNotExceptional();

						return Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse, webRequest.EndGetResponse, null);
					}).Unwrap()
					.ContinueWith(responseTask =>
					{
						responseTask.AssertNotExceptional();

						var responseStream = responseTask.Result.GetResponseStream();
						return responseStream.ReadJObject();
					}).Unwrap()
					.ContinueWith(resultsTask => ProcessResults(resultsTask.Result.Properties().Select(x => x.Value)))
					.IgnoreExceptions();
		}

		private static JsonSerializer CreateJsonSerializer()
		{
			return new JsonSerializer
			{
				Converters =
                    {
                        new JsonEnumConverter()
                    }
			};
		}

		private void ProcessResults(IEnumerable<JToken> results)
		{
			var list = new List<IncomingMessage>();
			var needAnotherUpdate = false;
			foreach (var value in results)
			{
				var readResults = new JsonSerializer().Deserialize<ReadResults>(new JTokenReader(value));
				needAnotherUpdate |= readResults.HasMoreResults;
				var lastMsg = readResults.Results.LastOrDefault();
				if (lastMsg == null)
					continue;

				lastEtagPerQeueue.AddOrUpdate(
					readResults.Queue,
					lastMsg.Id,
					(s, guid) => Max(lastMsg.Id, guid));

				Action<IRavenMQContext, OutgoingMessage> action;
				if (actionsPerQueue.TryGetValue(readResults.Queue, out action) == false)
					continue;
				foreach (var outgoingMessage in readResults.Results)
				{
					try
					{
						var ravenMQContext = new RavenMQContext(PublishMessagesAsync);
						action(ravenMQContext, outgoingMessage);
						list.AddRange(ravenMQContext.Messages);
					}
					catch(Exception e)
					{
						OnDeserializationError(e);
					}
				}
			}
			if (list.Count > 0)
				PublishMessagesAsync(list);
			if (needAnotherUpdate == false)
				return;
			UpdateAsync();
		}

		public event EventHandler<EventArgs<Exception>> DeserializationError;

		private void OnDeserializationError(Exception exception)
		{
			var onDeserializationError = DeserializationError;
			if(onDeserializationError!=null)
				onDeserializationError(this, new EventArgs<Exception>(exception));
		}

		public Task PublishMessagesAsync(IEnumerable<IncomingMessage> msgs)
		{
			var webRequest = (HttpWebRequest)WebRequest.Create(queueBulkEndpoint);
			webRequest.Method = "POST";
			webRequest.ContentType = "application/bson";
			webRequest.Accept = "application/bson";
			return Task.Factory.FromAsync<Stream>(webRequest.BeginGetRequestStream, webRequest.EndGetRequestStream, null)
					.ContinueWith(getReqStreamTask =>
					{
						getReqStreamTask.AssertNotExceptional();

						var enumerable = msgs.Select(msg => new EnqueueCommand
						{
							Message = msg
						}).Select(msg => JObject.FromObject(msg, CreateJsonSerializer()));
						return getReqStreamTask.Result.Write(new JArray(enumerable));
					}).Unwrap()
					.ContinueWith(writeRequestTask =>
					{
						writeRequestTask.AssertNotExceptional();

						return Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse, webRequest.EndGetResponse, null);
					}).Unwrap()
					.ContinueWith(responseTask =>
					{
						responseTask.AssertNotExceptional();

						responseTask.Result.Close();
					})
					.IgnoreExceptions();
		}

		private static Guid Max(Guid x, Guid y)
		{
			return x.CompareTo(y) > 0 ? x : y;
		}
	}
}