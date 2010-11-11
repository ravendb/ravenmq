using System;
using Raven.Http.Abstractions;
using Raven.Http.Extensions;
using RavenMQ.Data;
using RavenMQ.Extensions;

namespace RavenMQ.Server.Responders
{
    public class Queue : AbstractQueuesResponder
    {
        public override string UrlPattern
        {
            get { return "^/mq(/(queues/|streams/|remotes/).+)"; }
        }

        public override string[] SupportedVerbs
        {
            get { return new[] { "GET", "POST" }; }
        }

        public override void Respond(IHttpContext context)
        {
            var match = urlMatcher.Match(context.GetRequestUrl());
            
            var queue = match.Groups[1].Value;

            switch (context.Request.HttpMethod)
            {
                case "GET":
                    context.WriteJson(Queues.Statistics(queue));
                    break;
                case "POST":
                    var metadata = context.Request.Headers.FilterHeaders(isServerDocument: true);

                    var msgId = Queues.Enqueue(new IncomingMessage
                    {
                        Data = context.Request.InputStream.ReadData(),
                        Queue = queue,
                        Metadata = metadata,
                        TimeToLive = metadata.Property("Raven-Time-To-Live") != null
                            ? TimeSpan.FromSeconds(metadata.Value<int>("Raven-Time-To-Live"))
                            : TimeSpan.FromMinutes(5)
                    });

                    context.WriteJson(new {MsgId = msgId});
                    break;
            }
        }
    }
}