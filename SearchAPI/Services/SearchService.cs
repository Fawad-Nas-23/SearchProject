using Shared.Model;
using SearchLogic.Repository;
namespace SearchLogic.Services;
public class SearchService : ISearchService
{
    IDatabase mDatabase;

    public SearchService(IDatabase database)
    {
        mDatabase = database;
    }

    /* Perform search of documents containing words from query. The result will
     * contain details about amost maxAmount of documents.
     */
    public SearchResult Search(string[] query, int maxAmount)
    {
        Console.WriteLine($"[Search] Query: {string.Join(", ", query)}, MaxAmount: {maxAmount}");

        List<string> ignored;
        DateTime start = DateTime.Now;

        var wordIds = mDatabase.GetWordIds(query, out ignored);
        Console.WriteLine($"[Search] WordIds found: {wordIds.Count}, Ignored: {string.Join(", ", ignored)}");

        if (wordIds.Count == 0)
        {
            Console.WriteLine("[Search] No words found in index, returning empty result");
            return new SearchResult
            {
                Query = query,
                NoOfHits = 0,
                DocumentHits = new List<DocumentHit>(),
                Ignored = ignored,
                TimeUsed = DateTime.Now - start
            };
        }

        var docIds = mDatabase.GetDocuments(wordIds);
        Console.WriteLine($"[Search] Documents found: {docIds.Count}");

        var top = new List<int>();
        foreach (var p in docIds.GetRange(0, Math.Min(maxAmount, docIds.Count)))
            top.Add(p.docId);

        List<DocumentHit> docresult = new List<DocumentHit>();
        int idx = 0;
        foreach (var docId in top)
        {
            BEDocument doc = mDatabase.GetDocDetails(docId);
            var missing = mDatabase.WordsFromIds(mDatabase.GetMissing(doc.mId, wordIds));
            missing.AddRange(ignored);
            var docHit = new DocumentHit { Document = doc, NoOfHits = docIds[idx++].hits, Missing = missing };
            docresult.Add(docHit);
        }

        Console.WriteLine($"[Search] Returning {docresult.Count} document hits");
        return new SearchResult
        {
            Query = query,
            NoOfHits = docIds.Count,
            DocumentHits = docresult,
            Ignored = ignored,
            TimeUsed = DateTime.Now - start
        };
    }
}
