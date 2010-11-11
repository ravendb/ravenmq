using System;
using Raven.Abstractions.Data;

namespace Raven.Abstractions.Commands
{
    public class EnqueueCommand : ICommand
    {
        public IncomingMessage Message { get; set; }

        public CommandType Type
        {
            get { return CommandType.Enqueue; }
        }
    }
}