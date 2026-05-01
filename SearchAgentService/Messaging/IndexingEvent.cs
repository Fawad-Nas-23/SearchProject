namespace SearchAgentService.Messaging;

public class IndexingEvent
{
    public string EventType { get; set; } = "IndexingCompleted";
    public DateTime Timestamp { get; set; }
}