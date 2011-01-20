using System;
using Raven.MQ.Client;

namespace Raven.MQ.Tryouts
{
	class Program
	{
		static void Main()
		{

			var con = new RavenMQConnection(new Uri("http://AYENDEPC:8181/"));

			

			Console.ReadLine();
		}
	}


	public class User
	{
		public string Name { get; set; }
	}
}
