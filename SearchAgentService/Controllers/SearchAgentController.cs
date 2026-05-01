using Microsoft.AspNetCore.Mvc;
using SearchAgentService.Models;
using SearchAgentService.Repository;
using SearchAgentService.Services;

namespace SearchAgentService.Controllers;

/// <summary>
/// Controller ansvarlig for håndtering af SearchAgents:
/// - Oprette agent
/// - Hente alle agenter
/// - Slette agenter
/// - Manuelt trigge execution (til test/debug)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SearchAgentController : ControllerBase
{
    private readonly ISearchAgentRepository _repository;
    private readonly SearchAgentRunner _runner;
    private readonly ILogger<SearchAgentController> _logger;

    /// <summary>
    /// Constructor med dependency injection af:
    /// - Repository (database)
    /// - Runner (forretningslogik)
    /// - Logger (observability)
    /// </summary>
    public SearchAgentController(
        ISearchAgentRepository repository,
        SearchAgentRunner runner,
        ILogger<SearchAgentController> logger)
    {
        _repository = repository;
        _runner = runner;
        _logger = logger;
    }

    /// <summary>
    /// Opretter en ny SearchAgent i databasen
    /// </summary>
    [HttpPost]
    public ActionResult<SearchAgent> Create(SearchAgent agent)
    {
        // Valider input (undgår tomme søgninger)
        if (agent == null || agent.SearchWords == null || agent.SearchWords.Length == 0)
        {
            _logger.LogWarning("Invalid SearchAgent create request received");
            return BadRequest("SearchAgent must contain at least one search word.");
        }

        // Log hvad der bliver oprettet (vigtigt for debugging)
        _logger.LogInformation(
            "Creating SearchAgent | Email: {Email} | Words: {Words}",
            agent.Email,
            string.Join(",", agent.SearchWords));

        var created = _repository.Create(agent);

        // Log succesfuld oprettelse
        _logger.LogInformation(
            "SearchAgent created successfully | Id: {Id} | Email: {Email}",
            created.Id,
            created.Email);

        return Ok(created);
    }

    /// <summary>
    /// Returnerer alle SearchAgents fra databasen
    /// </summary>
    [HttpGet]
    public ActionResult<List<SearchAgent>> GetAll()
    {
        var agents = _repository.GetAll();

        // Giver overblik over hvor mange agenter der findes
        _logger.LogInformation(
            "Fetched all SearchAgents | Count: {Count}",
            agents.Count);

        return Ok(agents);
    }

    /// <summary>
    /// Sletter alle SearchAgents tilknyttet en email
    /// </summary>
    [HttpDelete("{email}")]
    public IActionResult DeleteByEmail(string email)
    {
        _logger.LogInformation(
            "Deleting SearchAgents by email | Email: {Email}",
            email);

        _repository.DeleteByEmail(email);

        _logger.LogInformation(
            "SearchAgents deleted for email | Email: {Email}",
            email);

        return Ok($"Deleted search agent(s) for email: {email}");
    }

    /// <summary>
    /// Manuelt endpoint til at køre alle SearchAgents
    /// Bruges primært til test (RabbitMQ skal ellers trigge dette automatisk)
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<List<SearchAgentRunResult>>> RunAgents()
    {
        _logger.LogInformation("Manual trigger: Running all SearchAgents");

        try
        {
            // Kør alle agents (kalder SearchAPI)
            var results = await _runner.RunAllAgentsAsync();

            // Tæl hvor mange der faktisk fandt noget
            var matches = results.Count(r => r.MatchFound);

            _logger.LogInformation(
                "SearchAgents run completed | Total: {Total} | Matches: {Matches}",
                results.Count,
                matches);

            return Ok(results);
        }
        catch (Exception ex)
        {
            // Kritisk: hvis dette fejler, virker hele systemet ikke
            _logger.LogError(ex, "Error while running SearchAgents");

            return StatusCode(500, "Error running search agents");
        }
    }
}