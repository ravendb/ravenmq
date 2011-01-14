using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Raven.Abstractions.Data;
using Raven.MQ.Client;

namespace Raven.MQ.Tryouts
{
	class Program
	{
		static void Main(string[] args)
		{
			using(var connection = RavenMQConnection.Connect("http://localhost:8181"))
			{
				connection.
					Subscribe<User>("/queues/abc", (context, message) => Console.WriteLine(message.Name));

				connection
					.StartPublishing
					.Add("/queues/abc", new User {Name = "Ayende"})
					.PublishAsync();

				Console.ReadLine();
			}
		}
	}

	public class User
	{
		public string Name { get; set; }
	}
}
