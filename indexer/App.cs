using NLog;
using Shared;
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

            crawler.IndexFilesIn(root, new List<string> { ".txt"});        

            TimeSpan used = DateTime.Now - start;
            Console.WriteLine("DONE! used " + used.TotalMilliseconds);

            Console.WriteLine($"Indexed {db.DocumentCounts} documents");

            var all = db.GetAllWords();
            Console.WriteLine($"Number of different words: {all.Count}");

            // New behaviour: show total occurrences and ask how many top words to display
            var totalOccurrences = db.GetTotalOccurrences();
            Console.WriteLine($"Total indexed word occurrences: {totalOccurrences}");

            if (crawler.DocumentsIndexed > 0)
            {
                PublishIndexingCompleted();

            }
            else
            {
                _logger.Info("No files indexed - no notification sent");
            }
            Console.Write("How many top words do you want to see? ");
            string input = Console.ReadLine();
            if (!int.TryParse(input, out int count) || count <= 0)
            {
                count = 10;
            }

            var top = db.GetTopWords(count);
            Console.WriteLine($"The top {count} words (most frequent first):");
            foreach (var (word, id, freq) in top)
            {
                Console.WriteLine($"<{word}, {id}> - {freq}");
            }
        }

        private IDatabase GetDatabase()
        {
            Console.Write("Use SQLite (1) or Postgres (2) database?");
            string input = Console.ReadLine();
            if (input.Equals("1"))
                return new DatabaseSqlite();
            else if (input.Equals("2"))
                return new DatabasePostgres();
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