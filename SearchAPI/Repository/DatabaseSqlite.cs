using System;
using System.Collections.Generic;
using Shared;
using Shared.Model;
using Microsoft.Data.Sqlite;

namespace SearchLogic.Repository;

public class DatabaseSqlite : IDatabase
{
    private SqliteConnection _connection;
    private Dictionary<string, int> mWords = null;
    private bool IgnoreCase = true;
    private readonly ILogger<DatabaseSqlite> _logger;

    public DatabaseSqlite(IConfiguration configuration, ILogger<DatabaseSqlite> logger)
    {
        _logger = logger;

        var connectionStringBuilder = new SqliteConnectionStringBuilder();
        var dbPath = configuration["SQLITE_DB"] ?? Paths.SQLITE_DATABASE_1;
        connectionStringBuilder.DataSource = dbPath;

        _logger.LogInformation("Connecting to SQLite database at {DbPath}", dbPath);

        try
        {
            _connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
            _connection.Open();
            _logger.LogInformation("SQLite connection opened successfully");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to open SQLite database at {DbPath}", dbPath);
            throw;
        }
    }

    private void Execute(string sql)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public List<(int docId, int hits)> GetDocuments(List<int> wordIds)
    {
        var res = new List<(int docId, int hits)>();

        var sql = "SELECT docId, COUNT(wordId) as count FROM Occ where ";
        sql += "wordId in " + AsString(wordIds) + " GROUP BY docId ";
        sql += "ORDER BY count DESC;";

        try
        {
            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = sql;

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var docId = reader.GetInt32(0);
                    var count = reader.GetInt32(1);
                    res.Add((docId, count));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDocuments query failed | WordIds: {WordIds}", string.Join(",", wordIds));
            throw;
        }

        return res;
    }

    private string AsString(List<int> x) => $"({string.Join(',', x)})";

    private Dictionary<string, int> GetAllWords()
    {
        _logger.LogDebug("Loading all words from database | IgnoreCase: {IgnoreCase}", IgnoreCase);

        var words = new Dictionary<string, int>(
            IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        try
        {
            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM word";

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var w = reader.GetString(1);
                    words.TryAdd(w, id);
                }
            }

            _logger.LogInformation("Word cache loaded | Count: {WordCount} | IgnoreCase: {IgnoreCase}",
                words.Count, IgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load words from database");
            throw;
        }

        return words;
    }

    public BEDocument GetDocDetails(int docId)
    {
        try
        {
            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = $"SELECT * FROM document where id = {docId}";

            using (var reader = selectCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var url = reader.GetString(1);
                    var idxTime = reader.GetString(2);
                    var creationTime = reader.GetString(3);
                    return new BEDocument { mId = id, mUrl = url, mIdxTime = idxTime, mCreationTime = creationTime };
                }
            }

            _logger.LogWarning("Document not found | DocId: {DocId}", docId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDocDetails failed | DocId: {DocId}", docId);
            throw;
        }
    }

    public List<int> GetMissing(int docId, List<int> wordIds)
    {
        var sql = "SELECT wordId FROM Occ where ";
        sql += "wordId in " + AsString(wordIds) + " AND docId = " + docId;
        sql += " ORDER BY wordId;";

        try
        {
            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = sql;

            List<int> present = new List<int>();

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var wordId = reader.GetInt32(0);
                    present.Add(wordId);
                }
            }

            var result = new List<int>(wordIds);
            foreach (var w in present)
                result.Remove(w);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMissing failed | DocId: {DocId}", docId);
            throw;
        }
    }

    public List<string> WordsFromIds(List<int> wordIds)
    {
        var sql = "SELECT name FROM Word where ";
        sql += "id in " + AsString(wordIds);

        try
        {
            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = sql;

            List<string> result = new List<string>();

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var wordId = reader.GetString(0);
                    result.Add(wordId);
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WordsFromIds failed | WordIds: {WordIds}", string.Join(",", wordIds));
            throw;
        }
    }

    public List<int> GetWordIds(string[] query, out List<string> outIgnored, bool caseSensitive)
    {
        if (mWords == null || IgnoreCase != !caseSensitive)
        {
            IgnoreCase = !caseSensitive;
            mWords = GetAllWords();
        }

        var res = new List<int>();
        var ignored = new List<string>();

        foreach (var aWord in query)
        {
            string key = caseSensitive ? aWord : aWord.ToLowerInvariant();

            if (mWords.ContainsKey(key))
                res.Add(mWords[key]);
            else
                ignored.Add(aWord);
        }

        outIgnored = ignored;
        return res;
    }
}