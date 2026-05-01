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

    /// <summary>
    /// Kører alle SearchAgents én ad gangen.
    /// Bruges både af manuelt endpoint og RabbitMQ subscriber.
    /// </summary>
    public async Task<List<SearchAgentRunResult>> RunAllAgentsAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting SearchAgent run");

        var agents = _repository.GetAll();
        var runResults = new List<SearchAgentRunResult>();

        _logger.LogInformation(
            "Fetched SearchAgents for execution | Count: {Count}",
            agents.Count);

        foreach (var agent in agents)
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

                // Kalder SearchAPI for at genbruge eksisterende søgelogik
                var response = await _httpClient.PostAsJsonAsync(
                    "http://localhost:5272/api/search",
                    request
                );

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "SearchAPI returned non-success status | AgentId: {AgentId} | StatusCode: {StatusCode}",
                        agent.Id,
                        response.StatusCode);

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
                        "Match found | AgentId: {AgentId} | Email: {Email} | Hits: {Hits}",
                        agent.Id,
                        agent.Email,
                        searchResult?.NoOfHits ?? 0);

                    // Agent slettes efter match, fordi den har opfyldt sit formål
                    _repository.DeleteById(agent.Id);

                    _logger.LogInformation(
                        "SearchAgent deleted after match | AgentId: {AgentId}",
                        agent.Id);
                }
                else
                {
                    _logger.LogInformation(
                        "No match found | AgentId: {AgentId} | Email: {Email}",
                        agent.Id,
                        agent.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while running SearchAgent | AgentId: {AgentId} | Email: {Email}",
                    agent.Id,
                    agent.Email);

                runResults.Add(new SearchAgentRunResult
                {
                    AgentId = agent.Id,
                    Email = agent.Email,
                    SearchWords = agent.SearchWords,
                    MatchFound = false,
                    NumberOfHits = 0,
                    SearchResult = null
                });
            }
            finally
            {
                agentStopwatch.Stop();

                _logger.LogInformation(
                    "SearchAgent execution finished | AgentId: {AgentId} | DurationMs: {DurationMs}",
                    agent.Id,
                    agentStopwatch.ElapsedMilliseconds);
            }

            // Throttle for ikke at overbelaste SearchAPI ved mange agents
            await Task.Delay(300);
        }

        stopwatch.Stop();

        var matches = runResults.Count(r => r.MatchFound);

        _logger.LogInformation(
            "SearchAgent run completed | Total: {Total} | Matches: {Matches} | DurationMs: {DurationMs}",
            runResults.Count,
            matches,
            stopwatch.ElapsedMilliseconds);

        return runResults;
    }
}