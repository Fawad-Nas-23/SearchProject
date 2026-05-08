using Microsoft.AspNetCore.Mvc;
using Shared.Model;

namespace OrchestratorAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrchestratorController : ControllerBase
{
    private readonly HttpClient _searchApiClient;
    private readonly HttpClient _agentClient;
    private readonly ILogger<OrchestratorController> _logger;
    public OrchestratorController(
        IHttpClientFactory httpClientFactory,
        ILogger<OrchestratorController> logger)
    {
        _searchApiClient = httpClientFactory.CreateClient("SearchAPI");
        _agentClient = httpClientFactory.CreateClient("SearchAgent");
        _logger = logger;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        _logger.LogInformation("Coordinator: forwarding search | Query: {Query}",
            string.Join(" ", request.Query));

        var response = await _searchApiClient.PostAsJsonAsync("/api/search", request);
        var content = await response.Content.ReadAsStringAsync();

        return Content(content, "application/json");
    }

    [HttpPost("agents")]
    public async Task<IActionResult> CreateAgent([FromBody] SearchAgentRequest request)
    {
        _logger.LogInformation("Coordinator: creating agent | Email: {Email}",
            request.Email);

        var response = await _agentClient.PostAsJsonAsync("/api/searchagent", request);
        var content = await response.Content.ReadAsStringAsync();

        return Content(content, "application/json");
    }

    [HttpGet("agents")]
    public async Task<IActionResult> GetAgents()
    {
        var response = await _agentClient.GetAsync("/api/searchagent");
        var content = await response.Content.ReadAsStringAsync();

        return Content(content, "application/json");
    }

    [HttpDelete("agents/{email}")]
    public async Task<IActionResult> DeleteAgents(string email)
    {
        var response = await _agentClient.DeleteAsync($"/api/searchagent/{email}");
        var content = await response.Content.ReadAsStringAsync();

        return Content(content, "application/json");
    }
}