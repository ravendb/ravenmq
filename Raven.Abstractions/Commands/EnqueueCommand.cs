using System;
using Raven.Abstractions.Data;

namespace Raven.Abstractions.Commands
{
    public class EnqueueCommand
    {
        public IncomingMessage Message { get; set; }
        public Guid MessageId { get; set; }
    }
}