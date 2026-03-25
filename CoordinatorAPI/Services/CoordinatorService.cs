using System.Net.Http.Json;
using Shared.Model;

namespace CoordinatorAPI.Services;

public interface ICoordinatorService
{
    Task<SearchResult> SearchAsync(string[] query, int maxAmount, bool caseSensitive);
}

public class CoordinatorService : ICoordinatorService
{
    private readonly HttpClient _httpClient;
    private readonly List<string> _instanceUrls;
    private readonly ILogger<CoordinatorService> _logger;

    public CoordinatorService(HttpClient httpClient, IConfiguration configuration, ILogger<CoordinatorService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var urls = configuration["SEARCH_INSTANCES"] ?? "http://localhost:5273,http://localhost:5274";
        _instanceUrls = urls.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        _logger.LogInformation("CoordinatorService initialized | Instances: {Instances}", string.Join(", ", _instanceUrls));
    }

    public async Task<SearchResult> SearchAsync(string[] query, int maxAmount, bool caseSensitive)
    {
        var request = new SearchRequest
        {
            Query = query,
            MaxAmount = maxAmount,
            CaseSensitive = caseSensitive
        };

        var queryStr = string.Join(" ", query);
        var start = DateTime.Now;

        _logger.LogInformation("Fan-out started | Query: {Query} | Instances: {InstanceCount}",
            queryStr, _instanceUrls.Count);

        // Fan out: query ALL instances in parallel
        var tasks = _instanceUrls.Select(url => QueryInstance(url, request, queryStr)).ToList();

        var responses = await Task.WhenAll(tasks);

        // Collect successful results
        var allResults = new List<SearchResult>();
        int failedCount = 0;

        for (int i = 0; i < responses.Length; i++)
        {
            if (responses[i] != null)
            {
                allResults.Add(responses[i]);
            }
            else
            {
                failedCount++;
            }
        }

        if (allResults.Count == 0)
        {
            _logger.LogError("All instances failed | Query: {Query}", queryStr);
            throw new Exception("All search instances failed.");
        }

        if (failedCount > 0)
        {
            _logger.LogWarning("Partial fan-out failure | Query: {Query} | Succeeded: {Success}/{Total}",
                queryStr, allResults.Count, _instanceUrls.Count);
        }

        // Braid: merge and sort
        var merged = BraidResults(allResults, query, maxAmount);
        merged.TimeUsed = DateTime.Now - start;

        _logger.LogInformation("Fan-out completed | Query: {Query} | MergedHits: {HitCount} | Ignored: {IgnoredCount} | Time: {TimeMs}ms",
            queryStr,
            merged.NoOfHits,
            merged.Ignored?.Count ?? 0,
            merged.TimeUsed.TotalMilliseconds);

        return merged;
    }

    private async Task<SearchResult?> QueryInstance(string url, SearchRequest request, string queryStr)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{url}/api/search", request);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Instance returned error | Instance: {Instance} | Status: {StatusCode} | Time: {TimeMs}ms | Query: {Query} | Body: {Body}",
                    url, (int)response.StatusCode, sw.ElapsedMilliseconds, queryStr, body);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<SearchResult>();

            _logger.LogDebug("Instance responded | Instance: {Instance} | Status: {StatusCode} | Time: {TimeMs}ms | Hits: {Hits}",
                url, (int)response.StatusCode, sw.ElapsedMilliseconds, result?.NoOfHits ?? 0);

            return result;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Instance unreachable | Instance: {Instance} | Time: {TimeMs}ms | Query: {Query}",
                url, sw.ElapsedMilliseconds, queryStr);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Instance timed out | Instance: {Instance} | Time: {TimeMs}ms | Query: {Query}",
                url, sw.ElapsedMilliseconds, queryStr);
            return null;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Unexpected error querying instance | Instance: {Instance} | Query: {Query}",
                url, queryStr);
            return null;
        }
    }

    private SearchResult BraidResults(List<SearchResult> results, string[] query, int maxAmount)
    {
        var seenUrls = new HashSet<string>();
        var allHits = new List<DocumentHit>();

        foreach (var r in results)
        {
            foreach (var hit in r.DocumentHits)
            {
                var url = hit.Document.mUrl;
                if (!seenUrls.Contains(url))
                {
                    seenUrls.Add(url);
                    allHits.Add(hit);
                }
            }
        }

        allHits.Sort((a, b) => b.NoOfHits.CompareTo(a.NoOfHits));

        var topHits = allHits.GetRange(0, Math.Min(maxAmount, allHits.Count));

        var ignoredSets = results.Select(r => new HashSet<string>(r.Ignored ?? new List<string>())).ToList();
        var commonIgnored = ignoredSets.Count > 0
            ? ignoredSets.Aggregate((a, b) => { a.IntersectWith(b); return a; })
            : new HashSet<string>();

        var totalHits = seenUrls.Count;

        _logger.LogDebug("Braid complete | UniqueUrls: {UniqueCount} | Duplicates removed: {DupCount} | Top returned: {TopCount}",
            totalHits,
            allHits.Count - totalHits + (allHits.Count - topHits.Count),
            topHits.Count);

        return new SearchResult
        {
            Query = query,
            NoOfHits = totalHits,
            DocumentHits = topHits,
            Ignored = commonIgnored.ToList(),
            TimeUsed = TimeSpan.Zero
        };
    }
}