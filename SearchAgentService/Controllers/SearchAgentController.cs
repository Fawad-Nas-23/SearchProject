using Microsoft.AspNetCore.Mvc;
using SearchAgentService.Models;
using SearchAgentService.Repository;
using SearchAgentService.Services;

namespace SearchAgentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchAgentController : ControllerBase
{
    private readonly ISearchAgentRepository _repository;
    private readonly SearchAgentRunner _runner;

    public SearchAgentController(
        ISearchAgentRepository repository,
        SearchAgentRunner runner)
    {
        _repository = repository;
        _runner = runner;
    }

    [HttpPost]
    public ActionResult<SearchAgent> Create(SearchAgent agent)
    {
        var created = _repository.Create(agent);
        return Ok(created);
    }

    [HttpGet]
    public ActionResult<List<SearchAgent>> GetAll()
    {
        return Ok(_repository.GetAll());
    }

    [HttpDelete("{email}")]
    public IActionResult DeleteByEmail(string email)
    {
        _repository.DeleteByEmail(email);
        return Ok($"Deleted search agent(s) for email: {email}");
    }

    [HttpPost("run")]
    public async Task<ActionResult<List<SearchAgentRunResult>>> RunAgents()
    {
        var results = await _runner.RunAllAgentsAsync();
        return Ok(results);
    }
}