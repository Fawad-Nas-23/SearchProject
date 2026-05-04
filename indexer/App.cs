using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using indexer.Messaging;

namespace Indexer;

public class App
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public void Run()
    {
        IDatabase db = GetDatabase();
        Crawler crawler = new Crawler(db);

        var root = new DirectoryInfo(Config.FOLDER);

        DateTime start = DateTime.Now;
        _logger.Info("Starting indexing of {Folder}", Config.FOLDER);

        crawler.IndexFilesIn(root, new List<string> { ".txt" });

        TimeSpan used = DateTime.Now - start;
        _logger.Info("Indexing complete in {DurationMs} ms", used.TotalMilliseconds);
        _logger.Info("Indexed {DocumentCount} documents", db.DocumentCounts);

        var all = db.GetAllWords();
        _logger.Info("Number of different words: {WordCount}", all.Count);

        var totalOccurrences = db.GetTotalOccurrences();
        _logger.Info("Total indexed word occurrences: {Occurrences}", totalOccurrences);

        if (crawler.DocumentsIndexed > 0)
        {
            PublishIndexingCompleted();
        }
        else
        {
            _logger.Info("No files indexed - no notification sent");
        }

        // Interactive top-words only when running locally
        if (Config.DATABASE_TYPE == "")
        {
            Console.Write("How many top words do you want to see? ");
            string input = Console.ReadLine();
            if (!int.TryParse(input, out int count) || count <= 0)
                count = 10;

            var top = db.GetTopWords(count);
            Console.WriteLine($"The top {count} words (most frequent first):");
            foreach (var (word, id, freq) in top)
                Console.WriteLine($"<{word}, {id}> - {freq}");
        }
    }

    private IDatabase GetDatabase()
    {
        var configured = Config.DATABASE_TYPE.ToLowerInvariant();
        if (configured == "sqlite") return new DatabaseSqlite();
        if (configured == "postgres") return new DatabasePostgres();

        // Interactive fallback for local dev
        Console.Write("Use SQLite (1) or Postgres (2) database?");
        string input = Console.ReadLine();
        if (input.Equals("1")) return new DatabaseSqlite();
        if (input.Equals("2")) return new DatabasePostgres();
        Console.WriteLine("Wrong input - try again...");
        return GetDatabase();
    }

    private void PublishIndexingCompleted()
    {
        using var publisher = new RabbitMQPublisher();

        var evt = new IndexingEvent
        {
            Timestamp = DateTime.UtcNow
        };

        publisher.Publish(evt);
    }
}