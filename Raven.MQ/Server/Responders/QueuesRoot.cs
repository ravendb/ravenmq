using System;
using Raven.Http.Abstractions;
using Raven.Http.Extensions;
using System.Linq;

namespace RavenMQ.Server.Responders
{
    public class QueuesRoot : QueuesResponder
    {
        public override string UrlPattern
        {
            get { return "^/mq/?$"; }
        }

        public override string[] SupportedVerbs
        {
            get { return new []{"GET"}; }
        }

        public override void Respond(IHttpContext context)
        {
            context.WriteJson(
                Queues
                    .GetQueueNames(context.GetStart(),context.GetPageSize(Configuration.MaxPageSize))
                    .Select(x=>Queues.Statistics(x))
                );
        }
    }
}