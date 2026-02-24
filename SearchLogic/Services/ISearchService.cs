using Shared.Model;

namespace SearchLogic.Services
{
    public interface ISearchService
    {
        SearchResult Search(string[] query, int maxAmount);
    }
}
