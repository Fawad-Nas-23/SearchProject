using Microsoft.AspNetCore.Mvc;
using CoordinatorAPI.Services;
using Shared.Model;

namespace CoordinatorAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoordinatorController : ControllerBase
{
    private readonly ICoordinatorService _coordinatorService;
    private readonly ILogger<CoordinatorController> _logger;

    public CoordinatorController(ICoordinatorService coordinatorService, ILogger<CoordinatorController> logger)
    {
        _coordinatorService = coordinatorService;
        _logger = logger;
    }

    [HttpGet("ping")]
    public ActionResult<string> Ping()
    {
        _logger.LogInformation("Coordinator ping received");
        return Ok("Coordinator is running");
    }

    [HttpPost("search")]
    public async Task<ActionResult<SearchResult>> Search([FromBody] SearchRequest request)
    {
        if (request.Query == null || request.Query.Length == 0)
        {
            _logger.LogWarning("Empty query received");
            return BadRequest("Query must contain at least one search term.");
        }

        try
        {
            _logger.LogInformation("Coordinator search started | Query: {Query} | MaxAmount: {MaxAmount} | CaseSensitive: {CaseSensitive}",
                string.Join(" ", request.Query),
                request.MaxAmount,
                request.CaseSensitive);

            var result = await _coordinatorService.SearchAsync(
                request.Query, request.MaxAmount, request.CaseSensitive);

            _logger.LogInformation("Coordinator search completed | Query: {Query} | Hits: {HitCount} | Time: {TimeMs}ms",
                string.Join(" ", request.Query),
                result.NoOfHits,
                result.TimeUsed.TotalMilliseconds);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Coordinator search failed | Query: {Query}",
                string.Join(" ", request.Query));

            return StatusCode(500, "Coordinator error occurred.");
        }
    }
}