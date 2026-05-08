var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Configuration.AddEnvironmentVariables();

var searchApiUrl = builder.Configuration["SEARCH_API_URL"] ?? "http://localhost:5272";
var agentUrl = builder.Configuration["SEARCHAGENT_URL"] ?? "http://localhost:5100";

builder.Services.AddHttpClient("SearchAPI", client =>
{
    client.BaseAddress = new Uri(searchApiUrl);
});

builder.Services.AddHttpClient("SearchAgent", client =>
{
    client.BaseAddress = new Uri(agentUrl);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.Run();