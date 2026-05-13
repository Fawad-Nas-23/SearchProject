using Microsoft.AspNetCore.Mvc;
using SearchAgentService.Models;
using SearchAgentService.Services;
using Shared.Model;
using Microsoft.FeatureManagement;

namespace SearchAgentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchAgentController : ControllerBase
{
    private readonly ISearchAgentService _service;
    private readonly ILogger<SearchAgentController> _logger;
    private readonly IFeatureManager _featureManager;

    public SearchAgentController(
        ISearchAgentService service,
        ILogger<SearchAgentController> logger,
        IFeatureManager featureManager)
    {
        _service = service;
        _logger = logger;
        _featureManager = featureManager;
    }

    [HttpPost]
    public async Task<ActionResult<SearchAgent>> Create(SearchAgentRequest request)
    {
        if (!await _featureManager.IsEnabledAsync(FeatureFlags.SearchAgent))
        {
            _logger.LogWarning("Search agent feature is disabled. Cannot create agent for email: {Email}", request.Email);
            return StatusCode(503, new { message = "Search agent functionality is currently disabled." });
        }

        try
        {
            var agent = new SearchAgent
            {
                Email = request.Email,
                SearchWords = request.SearchWords
            };

            var created = _service.Create(agent);
            return Ok(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<SearchAgent>>> GetAll()
    {
        if (!await _featureManager.IsEnabledAsync(FeatureFlags.SearchAgent))
        {
            _logger.LogWarning("Search agent feature is disabled. Cannot get agents.");
            return StatusCode(503, new { message = "Search agent functionality is currently disabled." });
        }

        return Ok(_service.GetAll());
    }

    [HttpDelete("{email}")]
    public async Task<IActionResult> DeleteByEmail(string email)
    {
        if (!await _featureManager.IsEnabledAsync(FeatureFlags.SearchAgent))
        {
            _logger.LogWarning("Search agent feature is disabled. Cannot delete agent for email: {Email}", email);
            return StatusCode(503, new { message = "Search agent functionality is currently disabled." });
        }

        _service.DeleteByEmail(email);
        return Ok($"Deleted search agent(s) for email: {email}");
    }

    [HttpPost("run")]
    public async Task<ActionResult<List<SearchAgentRunResult>>> RunAgents()
    {
        if (!await _featureManager.IsEnabledAsync(FeatureFlags.SearchAgent))
        {
            _logger.LogWarning("Search agent feature is disabled. Cannot run agents.");
            return StatusCode(503, new { message = "Search agent functionality is currently disabled." });
        }

        try
        {
            var results = await _service.RunAllAgentsAsync();
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running SearchAgents");
            return StatusCode(500, "Error running search agents");
        }
    }
}