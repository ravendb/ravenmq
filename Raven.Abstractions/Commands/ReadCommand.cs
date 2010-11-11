using System;
using System.Collections.Generic;
using Raven.Abstractions.Data;

namespace Raven.Abstractions.Commands
{
    public class ReadCommand
    {
        public string Queue { get; set; }
        public Guid LastMessageId { get; set; }
        public TimeSpan HideTimeout { get; set; }
        public IEnumerable<OutgoingMessage> Results { get; set; }
    }
}