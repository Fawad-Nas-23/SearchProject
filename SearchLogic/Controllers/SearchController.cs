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

        public SearchController(ISearchService searchLogic)
        {
            _searchLogic = searchLogic;
        }

        // POST api/search
        [HttpPost]
        public ActionResult<SearchResult> Search([FromBody] SearchRequest request)
        {
            if (request.Query == null || request.Query.Length == 0)
                return BadRequest("Query must contain at least one search term.");

            try
            {
                Console.WriteLine($"Starting search for: {string.Join(", ", request.Query)}");
                var result = _searchLogic.Search(request.Query, request.MaxAmount);
                Console.WriteLine("Search completed");
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
