using System.Collections.Generic;

namespace Raven.Abstractions.Data
{
    public class ReadResults
    {
        public IEnumerable<OutgoingMessage> Results { get; set; }
        public bool HasMoreResults { get; set; }
        public string Queue { get; set; }
    }
}