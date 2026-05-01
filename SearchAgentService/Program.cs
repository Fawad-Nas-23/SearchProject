using SearchAgentService.Repository;
using SearchAgentService.Services;
using SearchAgentService.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<SearchAgentRunner>();

builder.Services.AddSingleton<ISearchAgentRepository, SearchAgentPostgresRepository>();

builder.Services.AddHostedService<RabbitMQSubscriber>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();