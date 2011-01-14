using System;
using Raven.Http.Abstractions;
using Raven.Http.Extensions;

namespace RavenMQ.Server.Responders
{
	public class SubscriptionPort : AbstractQueuesResponder
	{
		public override string UrlPattern
		{
			get { return "^/subscriptionsPort$"; }
		}

		public override string[] SupportedVerbs
		{
			get { return new[]{"GET"}; }
		}

		public override void Respond(IHttpContext context)
		{
			context.WriteJson(new
			{
				Configuration.SubscriptionPort
			});
		}
	}
}