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

            _logger.LogInformation("Forwarding search to {Instance} | Query: {Query} | CaseSensitive: {CaseSensitive}",
                chosen,
                string.Join(" ", request.Query),
                request.CaseSensitive);

            try
            {
                using var client = new HttpClient();
                var json = System.Text.Json.JsonSerializer.Serialize(request);

                var sw = System.Diagnostics.Stopwatch.StartNew();

                var response = await client.PostAsync(
                    $"{chosen}/api/search",
                    new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                );

                sw.Stop();

                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Search response from {Instance} | Status: {StatusCode} | Time: {TimeMs}ms | Query: {Query}",
                        chosen,
                        (int)response.StatusCode,
                        sw.ElapsedMilliseconds,
                        string.Join(" ", request.Query));
                }
                else
                {
                    _logger.LogWarning("Search failed from {Instance} | Status: {StatusCode} | Time: {TimeMs}ms | Query: {Query}",
                        chosen,
                        (int)response.StatusCode,
                        sw.ElapsedMilliseconds,
                        string.Join(" ", request.Query));
                }

                return StatusCode((int)response.StatusCode, content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Instance unreachable | Instance: {Instance} | Query: {Query}",
                    chosen,
                    string.Join(" ", request.Query));

                return StatusCode(502, $"Search instance {chosen} is unavailable.");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timed out | Instance: {Instance} | Query: {Query}",
                    chosen,
                    string.Join(" ", request.Query));

                return StatusCode(504, $"Search instance {chosen} timed out.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error forwarding to {Instance} | Query: {Query}",
                    chosen,
                    string.Join(" ", request.Query));

                return StatusCode(500, "An internal error occurred.");
            }
        }
    }
}