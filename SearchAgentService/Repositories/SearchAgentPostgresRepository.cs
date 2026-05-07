using Npgsql;
using SearchAgentService.Models;
using Shared;

namespace SearchAgentService.Repository;

/// <summary>
/// Repository ansvarlig for databaseoperationer på SearchAgents.
/// Håndterer CRUD-operationer mod PostgreSQL.
/// </summary>
public class SearchAgentPostgresRepository : ISearchAgentRepository
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<SearchAgentPostgresRepository> _logger;

    /// <summary>
    /// Initialiserer databaseforbindelse og opretter tabel hvis den ikke findes
    /// </summary>
    public SearchAgentPostgresRepository(
        IConfiguration configuration,
        ILogger<SearchAgentPostgresRepository> logger)
    {
        _logger = logger;

        var connectionString = configuration["SEARCHAGENT_DATABASE"] ?? Paths.POSTGRES_AGENT_DATABASE;
        _logger.LogInformation("Connecting to PostgreSQL database at {DbPath}", connectionString);

        _logger.LogInformation(
            "Connecting to PostgreSQL database");

        try
        {
            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();

            _logger.LogInformation("PostgreSQL connection established successfully");

            CreateTableIfNotExists();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to connect to PostgreSQL database");
            throw;
        }
    }

    /// <summary>
    /// Sikrer at tabellen findes (idempotent operation)
    /// </summary>
    private void CreateTableIfNotExists()
    {
        using var cmd = _connection.CreateCommand();

        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS search_agents (
                id SERIAL PRIMARY KEY,
                email TEXT NOT NULL,
                search_words TEXT[] NOT NULL,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );
        """;

        try
        {
            cmd.ExecuteNonQuery();
            _logger.LogInformation("Ensured search_agents table exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create or verify search_agents table");
            throw;
        }
    }

    /// <summary>
    /// Henter alle SearchAgents fra databasen
    /// </summary>
    public List<SearchAgent> GetAll()
    {
        var agents = new List<SearchAgent>();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, email, search_words, created_at FROM search_agents ORDER BY id;";

        try
        {
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                agents.Add(new SearchAgent
                {
                    Id = reader.GetInt32(0),
                    Email = reader.GetString(1),
                    SearchWords = reader.GetFieldValue<string[]>(2),
                    CreatedAt = reader.GetDateTime(3)
                });
            }

            _logger.LogInformation(
                "Fetched SearchAgents from database | Count: {Count}",
                agents.Count);

            return agents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch SearchAgents from database");
            throw;
        }
    }

    /// <summary>
    /// Opretter en ny SearchAgent i databasen
    /// </summary>
    public SearchAgent Create(SearchAgent agent)
    {
        using var cmd = _connection.CreateCommand();

        cmd.CommandText = """
            INSERT INTO search_agents (email, search_words)
            VALUES (@email, @search_words)
            RETURNING id, created_at;
        """;

        cmd.Parameters.AddWithValue("email", agent.Email);
        cmd.Parameters.AddWithValue("search_words", agent.SearchWords);

        try
        {
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                agent.Id = reader.GetInt32(0);
                agent.CreatedAt = reader.GetDateTime(1);
            }

            _logger.LogInformation(
                "SearchAgent inserted into database | Id: {Id} | Email: {Email}",
                agent.Id,
                agent.Email);

            return agent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to insert SearchAgent | Email: {Email}",
                agent.Email);

            throw;
        }
    }

    /// <summary>
    /// Sletter en SearchAgent baseret på id
    /// </summary>
    public void DeleteById(int id)
    {
        using var cmd = _connection.CreateCommand();

        cmd.CommandText = "DELETE FROM search_agents WHERE id = @id;";
        cmd.Parameters.AddWithValue("id", id);

        try
        {
            var rows = cmd.ExecuteNonQuery();

            _logger.LogInformation(
                "Delete by Id executed | Id: {Id} | RowsAffected: {Rows}",
                id,
                rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete SearchAgent by Id | Id: {Id}",
                id);

            throw;
        }
    }

    /// <summary>
    /// Sletter alle SearchAgents for en given email
    /// </summary>
    public void DeleteByEmail(string email)
    {
        using var cmd = _connection.CreateCommand();

        cmd.CommandText = "DELETE FROM search_agents WHERE email = @email;";
        cmd.Parameters.AddWithValue("email", email);

        try
        {
            var rows = cmd.ExecuteNonQuery();

            _logger.LogInformation(
                "Delete by Email executed | Email: {Email} | RowsAffected: {Rows}",
                email,
                rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete SearchAgents by Email | Email: {Email}",
                email);

            throw;
        }
    }
}