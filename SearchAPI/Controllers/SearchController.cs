using Microsoft.AspNetCore.Mvc;
using SearchLogic.Services;
using Shared.Model;

namespace SearchLogic.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchLogic;
        private readonly string _instanceId;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ISearchService searchLogic, IConfiguration configuration, ILogger<SearchController> logger)
        {
            _searchLogic = searchLogic;
            _instanceId = configuration["INSTANCE"] ?? "Unknown";
            _logger = logger;
        }

        [HttpGet("ping")]
        public ActionResult<string> Ping()
        {
            _logger.LogInformation("Ping received on instance {Instance}", _instanceId);
            return Ok($"Search API is running. Instance: {_instanceId}");
        }

        // POST api/search
        [HttpPost]
        public ActionResult<SearchResult> Search([FromBody] SearchRequest request)
        {
            if (request.Query == null || request.Query.Length == 0)
            {
                _logger.LogWarning("Empty query received on instance {Instance}", _instanceId);
                return BadRequest("Query must contain at least one search term.");
            }

            try
            {
                _logger.LogInformation("Search started on {Instance} | Query: {Query} | MaxAmount: {MaxAmount} | CaseSensitive: {CaseSensitive}",
                    _instanceId,
                    string.Join(" ", request.Query),
                    request.MaxAmount,
                    request.CaseSensitive);

                var result = _searchLogic.Search(request.Query, request.MaxAmount, request.CaseSensitive);

                _logger.LogInformation("Search completed on {Instance} | Query: {Query} | Hits: {HitCount} | Time: {TimeMs}ms",
                    _instanceId,
                    string.Join(" ", request.Query),
                    result.NoOfHits,
                    result.TimeUsed.TotalMilliseconds);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search failed on {Instance} | Query: {Query}",
                    _instanceId,
                    string.Join(" ", request.Query));

                return StatusCode(500, "An internal error occurred.");
            }
        }
    }
}