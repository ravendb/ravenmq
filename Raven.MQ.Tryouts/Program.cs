using System;
using Raven.MQ.Client;

namespace Raven.MQ.Tryouts
{
	class Program
	{
		static void Main()
		{

			var con = new RavenMQConnection(new Uri("http://AYENDEPC:8181/"));

			con.Subscribe<User>("/queues/users/1234", (context, msg) => Console.WriteLine(msg.Name));

			con.Subscribe<User>("/streams/users/1234", (context, msg) => Console.WriteLine(msg.Name));

			for (int i = 0; i < 100; i++)
			{
				con.StartPublishing
					.Add("/streams/users/1234", new User {Name = "users/" + i})
					.Add("/queues/users/1234", new User {Name = "streams/" + i})
					.PublishAsync();
			}

			Console.ReadLine();
		}
	}


	public class User
	{
		public string Name { get; set; }
	}
}
