using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raven.Abstractions.Commands;
using Raven.Http.Abstractions;
using Raven.Http.Extensions;

namespace RavenMQ.Server.Responders
{
    public class Batch : AbstractQueuesResponder
    {
        public override string UrlPattern
        {
            get { return "^/bulk/?$"; }
        }

        public override string[] SupportedVerbs
        {
            get { return new[] { "POST" }; }
        }

        public override void Respond(IHttpContext context)
        {
        	var cmds = context.ReadJsonArray();
        	var typedCmds = new ICommand[cmds.Count];
        	for (int index = 0; index < cmds.Count; index++)
        	{
        		var cmd = cmds[index];
        		var value = cmd.Value<string>("Type").ToUpperInvariant();
        		switch (value)
        		{
        			case "CONSUME":
        				typedCmds[index] = (JsonDeserialization<ConsumeCommand>(cmd));
        				break;
        			case "ENQUEUE":
        				typedCmds[index] = (JsonDeserialization<EnqueueCommand>(cmd));
        				break;
        			case "READ":
        				typedCmds[index] = (JsonDeserialization<ReadCommand>(cmd));
        				break;
        			case "RESET":
        				typedCmds[index] = (JsonDeserialization<ResetCommand>(cmd));
        				break;
        			default:
        				throw new ArgumentException("Invalid cmd type: " + cmd.Value<string>("Type"));
        		}
        	}
        	context.Log(log =>
        	{
				if (log.IsDebugEnabled == false)
					return;

        		foreach (var typedCmd in typedCmds)
        		{
        			log.DebugFormat("\t{0} {1}", typedCmd.Type, typedCmd.ArgumentsForLog);
        		}
        	});
        	Queues.Batch(typedCmds);

        	var readResultses = typedCmds.OfType<ReadCommand>().Select(x => x.Result);
        	context.WriteJson(readResultses);
        }


    	private static T JsonDeserialization<T>(JToken self)
        {
            return (T)new JsonSerializer().Deserialize(new JTokenReader(self), typeof(T));
        }
    }
}