using System;
using Raven.Http;
using RavenMQ.Config;
using RavenMQ.Impl;
using RavenMQ.Server;

namespace Raven.MQ.Server
{
	public class RavenDbServer : IDisposable
	{
		private readonly Queues queues;
		private readonly HttpServer server;

        public Queues Queues
		{
			get { return queues; }
		}

		public HttpServer Server
		{
			get { return server; }
		}

		public RavenDbServer(InMemoryRavenConfiguration settings)
		{
			settings.LoadLoggingSettings();
			queues = new Queues(settings);

			try
			{
				server = new QueuesHttpServer(settings, queues);
				server.Start();
			}
			catch (Exception)
			{
				queues.Dispose();
				queues = null;
				
				throw;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			server.Dispose();
			queues.Dispose();
		}

		#endregion

	}
}
