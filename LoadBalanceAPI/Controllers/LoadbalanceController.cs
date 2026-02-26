using Microsoft.AspNetCore.Mvc;
using Shared.Model;
namespace LoadBalanceAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoadbalanceController : ControllerBase
    {
        private readonly ILogger<LoadbalanceController> _logger;
        private static readonly string[] _instances =
        [
            "http://localhost:5273", // Instance A
            "http://localhost:5274"  // Instance B
        ];
        private static readonly Random _random = new();

        public LoadbalanceController(ILogger<LoadbalanceController> logger)
        {
            _logger = logger;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            var chosen = _instances[_random.Next(_instances.Length)];
            _logger.LogInformation("Redirecting ping to {Instance}", chosen);
            return Redirect($"{chosen}/api/search/ping");
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {
            var chosen = _instances[_random.Next(_instances.Length)];
            _logger.LogInformation("Forwarding search to {Instance}", chosen);

            using var client = new HttpClient();
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var response = await client.PostAsync(
                $"{chosen}/api/search",
                new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            );

            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
    }
}