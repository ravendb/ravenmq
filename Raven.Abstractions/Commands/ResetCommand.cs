using System;

namespace Raven.Abstractions.Commands
{
    public class ResetCommand : ICommand
    {
        public Guid MessageId { get; set; }

    	public string ArgumentsForLog
    	{
			get { return MessageId.ToString(); }
    	}

    	public CommandType Type
        {
            get { return CommandType.Reset; }
        }
    }
}