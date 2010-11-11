using System;
using System.Collections.Generic;
using System.Linq;
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
                switch (cmd.Value<string>("Type"))
                {
                    case "Consume":
                        typedCmds[index] = (JsonDeserialization<ConsumeCommand>(cmd));
                        break;
                    case "Enqueue":
                        typedCmds[index] = (JsonDeserialization<EnqueueCommand>(cmd));
                        break;
                    case "Read":
                        typedCmds[index] = (JsonDeserialization<ReadCommand>(cmd));
                        break;
                    case "Reset":
                        typedCmds[index] = (JsonDeserialization<ResetCommand>(cmd));
                        break;
                }
            }

            Queues.Batch(typedCmds);

            context.WriteJson(
                typedCmds.OfType<ReadCommand>().Select(x=>x.Result)
                );

        }


        private static T JsonDeserialization<T>(JToken self)
        {
            return (T)new JsonSerializer().Deserialize(new JTokenReader(self), typeof(T));
        }
    }
}