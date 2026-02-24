using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Shared.Model;

namespace ConsoleSearch
{
    public class App
    {
        private readonly HttpClient _httpClient;

        public App()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:5272/")
            };

        }

        public async Task Run()
        {
            Console.WriteLine("Console Search (API Client)");

            while (true)
            {
                Console.WriteLine("Enter search terms - q for quit:");
                string input = Console.ReadLine();

                if (input?.ToLower() == "q")
                    break;

                var query = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                var request = new SearchRequest
                {
                    Query = query,
                    MaxAmount = 10
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    var response = await _httpClient.PostAsync("api/search", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        continue;
                    }

                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<SearchResult>(responseJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    PrintResult(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calling API: {ex.Message}");
                }
            }
        }

        private void PrintResult(SearchResult result)
        {
            if (result == null)
            {
                Console.WriteLine("No result returned.");
                return;
            }

            if (result.Ignored?.Count > 0)
                Console.WriteLine($"Ignored: {string.Join(',', result.Ignored)}");

            int idx = 1;
            foreach (var doc in result.DocumentHits)
            {
                Console.WriteLine($"{idx} : {doc.Document.mUrl} -- contains {doc.NoOfHits} search terms");
                Console.WriteLine("Index time: " + doc.Document.mIdxTime);
                Console.WriteLine($"Missing: {ArrayAsString(doc.Missing.ToArray())}");
                idx++;
            }

            Console.WriteLine($"Documents: {result.NoOfHits}. Time: {result.TimeUsed.TotalMilliseconds} ms");
        }

        private string ArrayAsString(string[] s)
            => s.Length == 0 ? "[]" : $"[{string.Join(',', s)}]";
    }
}