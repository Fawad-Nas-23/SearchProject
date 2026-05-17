using SearchAgentService.Models;

namespace SearchAgentService.Services;

public interface ISearchAgentService
{
    SearchAgent Create(SearchAgent agent);
    List<SearchAgent> GetAll();
    void DeleteByEmail(string email);
    Task<List<SearchAgentRunResult>> RunAllAgentsAsync();
}