using System;
using Raven.Abstractions.Data;

namespace Raven.Abstractions.Commands
{
    public class EnqueueCommand : ICommand
    {
        public IncomingMessage Message { get; set; }

    	public string ArgumentsForLog
    	{
    		get { return Message.Queue; }
    	}

    	public CommandType Type
        {
            get { return CommandType.Enqueue; }
        }
    }
}