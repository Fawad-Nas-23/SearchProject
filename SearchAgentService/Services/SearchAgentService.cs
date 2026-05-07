using System.Diagnostics;
using System.Net.Http.Json;
using SearchAgentService.Models;
using SearchAgentService.Repository;
using Shared.Model;

namespace SearchAgentService.Services;

/// <summary>
/// Kører alle gemte SearchAgents.
/// Henter agenter fra databasen, kalder SearchAPI og sletter agenten hvis der findes match.
/// </summary>
public class SearchAgentService : ISearchAgentService
{
    private readonly ISearchAgentRepository _repository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SearchAgentService> _logger;
    private readonly string _searchApiUrl;

    public SearchAgentService(
        ISearchAgentRepository repository,
        HttpClient httpClient,
        ILogger<SearchAgentService> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _httpClient = httpClient;
        _logger = logger;
        _searchApiUrl = configuration["SearchApiUrl"] ?? "http://localhost:5272/api/search";
    }

    public SearchAgent Create(SearchAgent agent)
    {
        if (agent == null || agent.SearchWords == null || agent.SearchWords.Length == 0)
            throw new ArgumentException("SearchAgent must contain at least one search word.");

        _logger.LogInformation(
            "Creating SearchAgent | Email: {Email} | Words: {Words}",
            agent.Email,
            string.Join(",", agent.SearchWords));

        var created = _repository.Create(agent);

        _logger.LogInformation(
            "SearchAgent created | Id: {Id} | Email: {Email}",
            created.Id,
            created.Email);

        return created;
    }

    public List<SearchAgent> GetAll()
    {
        var agents = _repository.GetAll();

        _logger.LogInformation(
            "Fetched all SearchAgents | Count: {Count}",
            agents.Count);

        return agents;
    }

    public void DeleteByEmail(string email)
    {
        _logger.LogInformation(
            "Deleting SearchAgents | Email: {Email}",
            email);

        _repository.DeleteByEmail(email);
    }

    public async Task<List<SearchAgentRunResult>> RunAllAgentsAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting SearchAgent run");

        var agents = _repository.GetAll();
        var runResults = new List<SearchAgentRunResult>();
        var agentsToDelete = new List<int>();

        _logger.LogInformation(
            "Fetched SearchAgents for execution | Count: {Count}",
            agents.Count);

        // Batch: kør 10 agenter parallelt ad gangen
        foreach (var batch in agents.Chunk(10))
        {
            var tasks = batch.Select(agent => RunSingleAgentAsync(agent));
            var batchResults = await Task.WhenAll(tasks);

            foreach (var result in batchResults)
            {
                runResults.Add(result);
                if (result.MatchFound)
                {
                    agentsToDelete.Add(result.AgentId);
                }
            }
        }

        // Slet KUN efter alt er kørt succesfuldt
        foreach (var id in agentsToDelete)
        {
            _repository.DeleteById(id);
            _logger.LogInformation("Deleted matched agent | AgentId: {AgentId}", id);
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "SearchAgent run completed | Total: {Total} | Matches: {Matches} | Deleted: {Deleted} | DurationMs: {DurationMs}",
            runResults.Count,
            runResults.Count(r => r.MatchFound),
            agentsToDelete.Count,
            stopwatch.ElapsedMilliseconds);

        return runResults;
    }

    private async Task<SearchAgentRunResult> RunSingleAgentAsync(SearchAgent agent)
    {
        var agentStopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Running SearchAgent | AgentId: {AgentId} | Email: {Email} | Words: {Words}",
                agent.Id,
                agent.Email,
                string.Join(",", agent.SearchWords));

            var request = new SearchRequest
            {
                Query = agent.SearchWords,
                MaxAmount = 20,
                CaseSensitive = false
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_searchApiUrl}/api/search",
                request 
            );           

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "SearchAPI returned non-success | AgentId: {AgentId} | StatusCode: {StatusCode}",
                    agent.Id,
                    response.StatusCode);

                return new SearchAgentRunResult
                {
                    AgentId = agent.Id,
                    Email = agent.Email,
                    SearchWords = agent.SearchWords,
                    MatchFound = false,
                    NumberOfHits = 0,
                    SearchResult = null
                };
            }

            var searchResult = await response.Content.ReadFromJsonAsync<SearchResult>();
            var matchFound = searchResult != null && searchResult.NoOfHits > 0;

            if (matchFound)
            {
                _logger.LogInformation(
                    "Match found | AgentId: {AgentId} | Hits: {Hits}",
                    agent.Id,
                    searchResult?.NoOfHits ?? 0);
            }
            else
            {
                _logger.LogInformation(
                    "No match | AgentId: {AgentId} | Email: {Email}",
                    agent.Id,
                    agent.Email);
            }

            return new SearchAgentRunResult
            {
                AgentId = agent.Id,
                Email = agent.Email,
                SearchWords = agent.SearchWords,
                MatchFound = matchFound,
                NumberOfHits = searchResult?.NoOfHits ?? 0,
                SearchResult = searchResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error running SearchAgent | AgentId: {AgentId}",
                agent.Id);

            return new SearchAgentRunResult
            {
                AgentId = agent.Id,
                Email = agent.Email,
                SearchWords = agent.SearchWords,
                MatchFound = false,
                NumberOfHits = 0,
                SearchResult = null
            };
        }
        finally
        {
            agentStopwatch.Stop();
            _logger.LogInformation(
                "Agent execution finished | AgentId: {AgentId} | DurationMs: {DurationMs}",
                agent.Id,
                agentStopwatch.ElapsedMilliseconds);
        }
    }
}