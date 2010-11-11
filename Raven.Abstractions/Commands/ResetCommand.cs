using System;

namespace Raven.Abstractions.Commands
{
    public class ResetCommand : ICommand
    {
        public Guid MessageId { get; set; }

        public CommandType Type
        {
            get { return CommandType.Reset; }
        }
    }
}