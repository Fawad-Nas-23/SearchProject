using SearchLogic.Repository;
using SearchLogic.Services;
using NLog.Web;
using OpenTelemetry.Metrics;
using StackExchange.Redis;
using IDatabase = SearchLogic.Repository.IDatabase;
using Instrumentation = SearchLogic.Metrics.Instrumentation;
namespace SearchLogic
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics.AddPrometheusExporter();
                    metrics.AddMeter("Microsoft.AspNetCore.Hosting",
                                     "Microsoft.AspNetCore.Server.Kestrel");
                    metrics.AddMeter(Instrumentation.MeterName);
                });

            // --- Setup Redis ---
            var redisEndpoint = builder.Configuration["RedisConnectionString"] ?? "localhost:6379";
            var password = builder.Configuration["REDIS_PASSWORD"] ?? "";
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                var configOptions = new ConfigurationOptions
                {
                    EndPoints = { redisEndpoint },
                    Password = password,
                    ConnectTimeout = 5000,
                    SyncTimeout = 5000
                };
                options.ConfigurationOptions = configOptions;
                options.InstanceName = "SearchAPI_";
            });

            builder.Services.AddControllers();
            builder.Services.AddScoped<IDatabase, DatabasePostgres>();
            //builder.Services.AddScoped<IDatabase, DatabaseSqlite>();
            builder.Services.AddScoped<ISearchService, SearchService>();
            builder.Services.AddSingleton<Instrumentation>();
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
            var configuredSqlite = builder.Configuration["SQLITE_DB"];

            app.UseOpenTelemetryPrometheusScrapingEndpoint();
            app.UseAuthorization();
            app.UseCors();
            app.MapControllers();
            app.Run();
        }
    }
}