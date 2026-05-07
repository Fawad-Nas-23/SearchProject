using Microsoft.AspNetCore.Mvc;
using SearchAgentService.Models;
using SearchAgentService.Services;

namespace SearchAgentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchAgentController : ControllerBase
{
    private readonly ISearchAgentService _service;
    private readonly ILogger<SearchAgentController> _logger;

    public SearchAgentController(
        ISearchAgentService service,
        ILogger<SearchAgentController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    public ActionResult<SearchAgent> Create(SearchAgent agent)
    {
        try
        {
            var created = _service.Create(agent);
            return Ok(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public ActionResult<List<SearchAgent>> GetAll()
    {
        return Ok(_service.GetAll());
    }

    [HttpDelete("{email}")]
    public IActionResult DeleteByEmail(string email)
    {
        _service.DeleteByEmail(email);
        return Ok($"Deleted search agent(s) for email: {email}");
    }

    [HttpPost("run")]
    public async Task<ActionResult<List<SearchAgentRunResult>>> RunAgents()
    {
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