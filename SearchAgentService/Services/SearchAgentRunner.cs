using System.Net.Http.Json;
using SearchAgentService.Models;
using SearchAgentService.Repository;
using Shared.Model;

namespace SearchAgentService.Services;

public class SearchAgentRunner
{
    private readonly ISearchAgentRepository _repository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SearchAgentRunner> _logger;

    public SearchAgentRunner(
        ISearchAgentRepository repository,
        HttpClient httpClient,
        ILogger<SearchAgentRunner> logger)
    {
        _repository = repository;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<SearchAgentRunResult>> RunAllAgentsAsync()
    {
        var agents = _repository.GetAll();
        var runResults = new List<SearchAgentRunResult>();

        foreach (var agent in agents)
        {
            var request = new SearchRequest
            {
                Query = agent.SearchWords,
                MaxAmount = 20,
                CaseSensitive = false
            };

            var response = await _httpClient.PostAsJsonAsync(
                "http://localhost:5272/api/search",
                request
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Search failed for agent {Id}", agent.Id);

                runResults.Add(new SearchAgentRunResult
                {
                    AgentId = agent.Id,
                    Email = agent.Email,
                    SearchWords = agent.SearchWords,
                    MatchFound = false,
                    NumberOfHits = 0,
                    SearchResult = null
                });

                continue;
            }

            var searchResult = await response.Content.ReadFromJsonAsync<SearchResult>();

            var matchFound = searchResult != null && searchResult.NoOfHits > 0;

            runResults.Add(new SearchAgentRunResult
            {
                AgentId = agent.Id,
                Email = agent.Email,
                SearchWords = agent.SearchWords,
                MatchFound = matchFound,
                NumberOfHits = searchResult?.NoOfHits ?? 0,
                SearchResult = searchResult
            });

            if (matchFound)
            {
                _logger.LogInformation(
                    "Match found for {Email}. Deleting agent {Id}",
                    agent.Email,
                    agent.Id
                );

                _repository.DeleteById(agent.Id);
            }

            await Task.Delay(300);
        }

        return runResults;
    }
}