using System;
using Raven.Abstractions.Data;
using Raven.Http.Abstractions;
using Raven.Http.Extensions;
using System.Linq;
using RavenMQ.Impl;

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
            TimeSpan timeout = context.GetTimeout(TimeSpan.FromMinutes(3));

            context.WriteJson(Queues.Read(new ReadRequest
            {
              HideTimeout  = timeout,
              LastMessageId = etag,
              PageSize = pageSize,
              Queue = queue
            }));
        }
    }
}