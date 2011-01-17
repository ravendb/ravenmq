using System;
using System.Threading;
using Raven.Http;
using Raven.MQ.Client;
using Raven.MQ.Server;
using RavenMQ.Config;
using Xunit;

namespace Raven.MQ.Tests.Client
{
	public class TypedMessages: IDisposable
    {
        private readonly RavenMqServer ravenMqServer;
        private readonly InMemoryRavenConfiguration configuration;

        public TypedMessages()
        {
            configuration = new RavenConfiguration
            {
                RunInMemory = true,
                AnonymousUserAccessMode = AnonymousUserAccessMode.All
            };
			NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(configuration.Port);
            ravenMqServer = new RavenMqServer(configuration);
        }

		
        public void Dispose()
        {
            ravenMqServer.Dispose();
        }

		[Fact]
		public void Publishing_string()
		{
			using (var connection = new RavenMQConnection(new Uri(configuration.ServerUrl)))
			{
				string str = null;
				var slim = new ManualResetEventSlim(false);
				connection.DeserializationError += (sender, args) =>
				{
					str = args.Value.ToString();
					slim.Set();
				};
			
				connection.Subscribe<string>("/queues/str", (context, s) =>
				{
					str = s;
					slim.Set();
				});

				connection.ConnectToServerTask.Wait();

				connection.StartPublishing
					.Add("/queues/str", "abc")
					.PublishAsync().Wait();


				slim.Wait();
				Assert.Equal("abc", str);
			}
		}

		[Fact]
		public void Publishing_DateTime()
		{
			using (var connection = new RavenMQConnection(new Uri(configuration.ServerUrl)))
			{
				DateTime dt = DateTime.MinValue;
				var slim = new ManualResetEventSlim(false);
				
				connection.StartPublishing
					.Add("/queues/dt", new DateTime(2010,1,1))
					.PublishAsync(); 
				
				connection.Subscribe<DateTime>("/queues/dt", (context, s) =>
				{
					dt = s;
					slim.Set();
				});


				slim.Wait();
				Assert.Equal(new DateTime(2010, 1, 1), dt);
			}
		}

		[Fact]
		public void Publishing_Guid()
		{
			using (var connection = new RavenMQConnection(new Uri(configuration.ServerUrl)))
			{
				Exception e = null;
				var slim = new ManualResetEventSlim(false);
				connection.DeserializationError += (sender, args) =>
				{
					e = args.Value;
					slim.Set();
				};
				Guid expected = new Guid("368B3B18-ACCD-4D95-8632-82A93E9E15DE");
				Guid dt = Guid.Empty;

				connection.StartPublishing
					.Add("/queues/gd", expected)
					.PublishAsync();

				connection.Subscribe<Guid>("/queues/gd", (context, s) =>
				{
					dt = s;
					slim.Set();
				});


				slim.Wait();
				Assert.Null(e);
				Assert.Equal(expected, dt);
			}
		}

		public class User
		{
			public string Name { get; set; }
		}

		[Fact]
		public void Publishing_User()
		{
			using (var connection = new RavenMQConnection(new Uri(configuration.ServerUrl)))
			{
				Exception e = null;
				var slim = new ManualResetEventSlim(false);
				connection.DeserializationError += (sender, args) =>
				{
					e = args.Value;
					slim.Set();
				};
				User u = null;
				connection.StartPublishing
					.Add("/queues/User", new User{ Name = "Ayende"})
					.PublishAsync();

				connection.Subscribe<User>("/queues/User", (context, s) =>
				{
					u = s;
					slim.Set();
				});


				slim.Wait();
				Assert.Null(e);
				Assert.Equal(u.Name, "Ayende");
			}
		}
		
		[Fact]
		public void Queues_are_incase_sensitive()
		{
			using (var connection = new RavenMQConnection(new Uri(configuration.ServerUrl)))
			{
				Exception e = null;
				var slim = new ManualResetEventSlim(false);
				connection.DeserializationError += (sender, args) =>
				{
					e = args.Value;
					slim.Set();
				};
				User u = null;
				connection.StartPublishing
					.Add("/queues/User", new User { Name = "Ayende" })
					.PublishAsync();

				connection.Subscribe<User>("/queues/USER", (context, s) =>
				{
					u = s;
					slim.Set();
				});


				slim.Wait();
				Assert.Null(e);
				Assert.Equal(u.Name, "Ayende");
			}
		}

		[Fact]
		public void Publishing_string_before_subscription()
		{
			using (var connection = new RavenMQConnection(new Uri(configuration.ServerUrl)))
			{
				string str = null;
				var slim = new ManualResetEventSlim(false);
				connection.DeserializationError += (sender, args) =>
				{
					str = args.Value.ToString();
					slim.Set();
				};

				connection.StartPublishing
					.Add("/queues/str", "abc")
					.PublishAsync().Wait();

				connection.Subscribe<string>("/queues/str", (context, s) =>
				{
					str = s;
					slim.Set();
				});

				connection.ConnectToServerTask.Wait();

				slim.Wait(TimeSpan.FromSeconds(1));
				Assert.Equal("abc", str);
			}
		}
	}
}