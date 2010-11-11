using System;

namespace Raven.Abstractions.Commands
{
    public class ConsumeCommand : ICommand
    {
        public Guid MessageId { get; set; }

        public CommandType Type
        {
            get { return CommandType.Consume; }
        }
    }
}