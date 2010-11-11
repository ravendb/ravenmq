using System;
using Raven.Http.Abstractions;
using Raven.Http.Extensions;
using System.Linq;

namespace RavenMQ.Server.Responders
{
    public class Messages : AbstractQueuesResponder
    {
        public override string UrlPattern
        {
            get { return "^/msgs(/(queues/|streams/|remotes/).+)"; }
        }

        public override string[] SupportedVerbs
        {
            get { return new[] { "GET", }; }
        }

        public override void Respond(IHttpContext context)
        {
            var match = urlMatcher.Match(context.GetRequestUrl());

            var queue = match.Groups[1].Value;
            var etag = context.GetEtagFromQueryString() ?? Guid.Empty;
            var pageSize = context.GetPageSize(Configuration.MaxPageSize);

            context.WriteJson(Queues.Read(queue, etag).Take(pageSize));
        }
    }
}