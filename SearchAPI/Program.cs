using SearchLogic.Repository;
using SearchLogic.Services;
namespace SearchLogic
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

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

            app.UseAuthorization();
            app.UseCors();

            app.MapControllers();

            app.Run();
        }
    }
}
