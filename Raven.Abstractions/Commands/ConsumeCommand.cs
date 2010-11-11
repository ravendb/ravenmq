using System;

namespace Raven.Abstractions.Commands
{
    public class ConsumeCommand
    {
        public Guid MessageId { get; set; }
    }
}