using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
    public class RavenMQConnection : IDisposable, IClientIntegration
    {
        private readonly Uri queueBulkEndpoint;
        private readonly IPEndPoint subscriptionEndpoint;
        private ClientConnection clientConnection;

        private readonly ConcurrentDictionary<string, Guid> lastEtagPerQeueue =
            new ConcurrentDictionary<string, Guid>(StringComparer.InvariantCultureIgnoreCase);

        private readonly ConcurrentDictionary<string, Action<IRavenMQContext, OutgoingMessage>> actionsPerQueue =
            new ConcurrentDictionary<string, Action<IRavenMQContext, OutgoingMessage>>(StringComparer.InvariantCultureIgnoreCase);

        private Task connectToServerTask;
    	private bool disposed;

    	public RavenMQConnection(Uri queueEndpoint, IPEndPoint subscriptionEndpoint)
        {
            queueBulkEndpoint = new UriBuilder(queueEndpoint)
            {
                Path = queueEndpoint.AbsolutePath + "/bulk"
            }.Uri;
            this.subscriptionEndpoint = subscriptionEndpoint;
            TryReconnecting();
        }

    	public Task ConnectToServerTask
    	{
    		get { return connectToServerTask; }
    	}

    	public IDisposable Subscribe(string queue, Action<IRavenMQContext, OutgoingMessage> action)
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

            clientConnection = new ClientConnection(subscriptionEndpoint, this);
            connectToServerTask = clientConnection.Connect()
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
						TaskEx.Delay(TimeSpan.FromSeconds(3))
							.ContinueWith(_ => TryReconnecting());
					return task;
				})
				.Unwrap();
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
                    .ContinueWith(resultsTask => ProcessResults(resultsTask.Result.Properties().Select(x=>x.Value)))
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
                if(lastMsg == null)
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
                    catch { }
                }
            }
            if (list.Count > 0)
                PublishMessagesAsync(list);
            if (needAnotherUpdate == false)
                return;
            UpdateAsync();
        }

		public Task PublishAsync(IncomingMessage msg)
		{
			return PublishMessagesAsync(new[] {msg,});
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