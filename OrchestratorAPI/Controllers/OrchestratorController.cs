using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Shared.Model;

namespace OrchestratorAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrchestratorController : ControllerBase
{
    private readonly HttpClient _searchApiClient;
    private readonly HttpClient _agentClient;
    private readonly ILogger<OrchestratorController> _logger;
    private readonly IFeatureManager _featureManager;

    public OrchestratorController(
        IHttpClientFactory httpClientFactory,
        ILogger<OrchestratorController> logger,
        IFeatureManager featureManager)
    {
        _searchApiClient = httpClientFactory.CreateClient("SearchAPI");
        _agentClient = httpClientFactory.CreateClient("SearchAgent");
        _logger = logger;
        _featureManager = featureManager;
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

        if (!await _featureManager.IsEnabledAsync(FeatureFlags.SearchAgent))
        {
            _logger.LogWarning("Search agent feature is disabled. Cannot create agent for email: {Email}", request.Email);
            return StatusCode(503, new
            {
                message = "Search agent functionality is currently disabled."
            });
            
        }

        var response = await _agentClient.PostAsJsonAsync("/api/searchagent", request);
        var content = await response.Content.ReadAsStringAsync();

        return Content(content, "application/json");
    }

    [HttpGet("agents")]
    public async Task<IActionResult> GetAgents()
    {
        if (!await _featureManager.IsEnabledAsync(FeatureFlags.SearchAgent))
        {
            _logger.LogWarning("Search agent feature is disabled. Cannot get agents.");
            return StatusCode(503, new
            {
                message = "Search agent functionality is currently disabled."
            });

        }
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