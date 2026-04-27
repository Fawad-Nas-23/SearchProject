using System;
using System.Collections.Generic;
using System.Text;

namespace indexer.Messaging
{
    public class IndexingEvent
    {
        public string EventType { get; set; } = "IndexingCompleted";
        public DateTime Timestamp { get; set; }

    }
}
