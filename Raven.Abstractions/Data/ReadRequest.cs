using System;

namespace Raven.Abstractions.Data
{
    public class ReadRequest
    {
        public ReadRequest()
        {
            HideTimeout = TimeSpan.FromMinutes(3);
            PageSize = 25;
        }
        public string Queue { get; set; }

        public Guid LastMessageId { get; set; }

        public TimeSpan HideTimeout { get; set; }

        public int PageSize { get; set; }
    }
}