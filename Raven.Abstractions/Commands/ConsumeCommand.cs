using System;

namespace Raven.Abstractions.Commands
{
    public class ConsumeCommand : ICommand
    {
        public Guid MessageId { get; set; }

    	public string ArgumentsForLog
    	{
			get { return MessageId.ToString(); }
    	}

    	public CommandType Type
        {
            get { return CommandType.Consume; }
        }
    }
}