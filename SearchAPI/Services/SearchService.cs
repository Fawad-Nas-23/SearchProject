using Shared.Model;
using SearchLogic.Repository;

namespace SearchLogic.Services;

public class SearchService : ISearchService
{
    private readonly IDatabase _database;
    private readonly ILogger<SearchService> _logger;

    public SearchService(IDatabase database, ILogger<SearchService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public SearchResult Search(string[] query, int maxAmount, bool caseSensitive)
    {
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

        _logger.LogDebug("Word lookup complete | WordIds: {WordIdCount} | Ignored: {IgnoredCount}",
            wordIds.Count, ignored.Count);

        var docIds = _database.GetDocuments(wordIds);

        _logger.LogDebug("Document lookup complete | TotalDocs: {DocCount}", docIds.Count);

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

        _logger.LogInformation("Search complete | Query: {Query} | Results: {ResultCount}/{TotalHits} | Time: {TimeMs}ms",
            string.Join(" ", query), docresult.Count, docIds.Count, elapsed.TotalMilliseconds);

        return new SearchResult
        {
            Query = query,
            NoOfHits = docIds.Count,
            DocumentHits = docresult,
            Ignored = ignored,
            TimeUsed = elapsed
        };
    }
}