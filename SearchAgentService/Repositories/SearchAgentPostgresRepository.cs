using Npgsql;
using SearchAgentService.Models;

namespace SearchAgentService.Repository;

public class SearchAgentPostgresRepository : ISearchAgentRepository
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<SearchAgentPostgresRepository> _logger;

    public SearchAgentPostgresRepository(IConfiguration configuration, ILogger<SearchAgentPostgresRepository> logger)
    {
        _logger = logger;

        var connectionString = configuration["POSTGRES_DATABASE"]
            ?? "Host=127.0.0.1;Port=5433;Username=postgres;Password=1234;Database=SearchDB";

        _logger.LogInformation("Connecting to PostgreSQL database at {ConnectionString}", connectionString);

        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();

        CreateTableIfNotExists();
    }

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

        cmd.ExecuteNonQuery();
    }

    public List<SearchAgent> GetAll()
    {
        var agents = new List<SearchAgent>();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, email, search_words, created_at FROM search_agents ORDER BY id;";

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

        return agents;
    }

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

        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            agent.Id = reader.GetInt32(0);
            agent.CreatedAt = reader.GetDateTime(1);
        }

        return agent;
    }

    public void DeleteById(int id)
    {
        using var cmd = _connection.CreateCommand();

        cmd.CommandText = "DELETE FROM search_agents WHERE id = @id;";
        cmd.Parameters.AddWithValue("id", id);

        cmd.ExecuteNonQuery();
    }

    public void DeleteByEmail(string email)
    {
        using var cmd = _connection.CreateCommand();

        cmd.CommandText = "DELETE FROM search_agents WHERE email = @email;";
        cmd.Parameters.AddWithValue("email", email);

        cmd.ExecuteNonQuery();
    }
}