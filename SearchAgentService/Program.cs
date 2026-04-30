using SearchAgentService.Repository;
using SearchAgentService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<SearchAgentRunner>();

builder.Services.AddSingleton<ISearchAgentRepository, SearchAgentPostgresRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();