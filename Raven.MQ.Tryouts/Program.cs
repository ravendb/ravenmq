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
			using(var connection = RavenMQConnection.Connect("http://reduction:8181"))
			{
				connection.Subscribe("/queues/abc", (context, message) => 
					Console.WriteLine(Encoding.UTF8.GetString(message.Data)));

				connection.PublishAsync(new IncomingMessage
				{
					Queue = "/queues/abc",
					Data = Encoding.UTF8.GetBytes("Hello Ravens")
				});

				Console.ReadLine();
			}
		}
	}
}
