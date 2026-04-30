using Shared.Model;

namespace SearchAgentService.Models;

public class SearchAgentRunResult
{
    public int AgentId { get; set; }
    public string Email { get; set; } = "";
    public string[] SearchWords { get; set; } = [];
    public bool MatchFound { get; set; }
    public int NumberOfHits { get; set; }
    public SearchResult? SearchResult { get; set; }
}