namespace SearchAgentService.Models;

public class SearchAgent
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string[] SearchWords { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}