using Shared.Model;
using SearchLogic.Repository;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using SearchLogic.Metrics;

namespace SearchLogic.Services;

public class SearchService : ISearchService
{
    private readonly IDatabase _database;
    private readonly ILogger<SearchService> _logger;
    private readonly IDistributedCache _cache;
    private readonly IInstrumentation _instrumentation;
    public SearchService(IDatabase database, ILogger<SearchService> logger, IDistributedCache cache, Instrumentation instrumentation)
    {
        _database = database;
        _logger = logger;
        _cache = cache;
        _instrumentation = instrumentation;
    }

    public SearchResult Search(string[] query, int maxAmount, bool caseSensitive)
    {
        // --- Cache lookup ---
        var cacheKey = $"{string.Join("_", query)}_{maxAmount}_{caseSensitive}";
        var cached = _cache.GetString(cacheKey);
        if (cached != null)
        {
            _logger.LogInformation("Cache HIT | Query: {Query}", string.Join(" ", query));
            _instrumentation.RecordCacheHit();
            return JsonSerializer.Deserialize<SearchResult>(cached)!;
        }
        _logger.LogInformation("Cache MISS | Query: {Query}", string.Join(" ", query));
        _instrumentation.RecordCacheMiss();

        _logger.LogDebug("Search invoked | Query: {Query} | MaxAmount: {MaxAmount} | CaseSensitive: {CaseSensitive}",
            string.Join(" ", query), maxAmount, caseSensitive);

        List<string> ignored;
        DateTime start = DateTime.Now;

        var wordIds = _database.GetWordIds(query, out ignored, caseSensitive);

        if (ignored.Count > 0)
        {
            _logger.LogWarning("Unknown words ignored | Ignored: {IgnoredWords} | Query: {Query}",
                string.Join(", ", ignored), string.Join(" ", query));
        }

        if (wordIds.Count == 0)
        {
            _logger.LogInformation("No indexed words matched | Query: {Query}", string.Join(" ", query));

            return new SearchResult
            {
                Query = query,
                NoOfHits = 0,
                DocumentHits = new List<DocumentHit>(),
                Ignored = ignored,
                TimeUsed = DateTime.Now - start
            };
        }

        var docIds = _database.GetDocuments(wordIds);

        var top = new List<int>();
        foreach (var p in docIds.GetRange(0, Math.Min(maxAmount, docIds.Count)))
            top.Add(p.docId);

        List<DocumentHit> docresult = new List<DocumentHit>();
        int idx = 0;

        foreach (var docId in top)
        {
            try
            {
                BEDocument doc = _database.GetDocDetails(docId);

                if (doc == null)
                {
                    _logger.LogError("Document details not found for DocId: {DocId}", docId);
                    continue;
                }

                var missing = _database.WordsFromIds(_database.GetMissing(doc.mId, wordIds));
                missing.AddRange(ignored);

                var docHit = new DocumentHit { Document = doc, NoOfHits = docIds[idx++].hits, Missing = missing };
                docresult.Add(docHit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get details for DocId: {DocId}", docId);
                idx++;
            }
        }

        var elapsed = DateTime.Now - start;

        var result = new SearchResult
        {
            Query = query,
            NoOfHits = docIds.Count,
            DocumentHits = docresult,
            Ignored = ignored,
            TimeUsed = elapsed
        };

        // --- Gem i cache i 5 minutter ---
        _cache.SetString(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        _logger.LogInformation("Search complete | Query: {Query} | Results: {ResultCount}/{TotalHits} | Time: {TimeMs}ms",
            string.Join(" ", query), docresult.Count, docIds.Count, elapsed.TotalMilliseconds);

        return result;
    }
}