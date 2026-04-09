using SearchLogic.Repository;
using SearchLogic.Services;
using NLog.Web;
using OpenTelemetry.Metrics;
namespace SearchLogic
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // --- NYT: Setup NLog ---
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            // --- NYT: Setup Metrikker ---
            // --- NYT: Setup Metrikker ---
            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddPrometheusExporter();
                    metrics.AddMeter("Microsoft.AspNetCore.Hosting",
                                     "Microsoft.AspNetCore.Server.Kestrel");
                });
            builder.Services.AddControllers();
            builder.Services.AddScoped<IDatabase, DatabaseSqlite>();
            builder.Services.AddScoped<ISearchService, SearchService>();
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            builder.Configuration.AddEnvironmentVariables();
            var app = builder.Build();

            Console.WriteLine($"ENV INSTANCE = {Environment.GetEnvironmentVariable("INSTANCE")}");
            Console.WriteLine($"CONFIG INSTANCE = {builder.Configuration["INSTANCE"]}");
            var configuredSqlite = builder.Configuration["SQLITE_DB"];
            Console.WriteLine($"Configured SQLITE_DB (from env/config): {configuredSqlite ?? "<not set>"}");


            // Configure the HTTP request pipeline.

            //app.UseHttpsRedirection();
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
            app.UseAuthorization();
            app.UseCors();

            app.MapControllers();

            app.Run();
        }
    }
}
