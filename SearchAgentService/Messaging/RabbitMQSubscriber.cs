using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;
using SearchAgentService.Services;
using SearchAgentService.Messaging;
using Microsoft.FeatureManagement;
using Shared.Model;

namespace SearchAgentService.Messaging;

public class RabbitMQSubscriber : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMQSubscriber> _logger;
    private readonly string _queueName;
    private readonly string _hostName;
    private readonly int _port;
    private readonly string _userName;
    private readonly string _password;

    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMQSubscriber(
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMQSubscriber> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _hostName = configuration["RABBITMQ_HOST"] ?? "localhost";
        _port = int.TryParse(configuration["RABBITMQ_PORT"], out var port) ? port : 5672;
        _userName = configuration["RABBITMQ_USER"] ?? "guest";
        _password = configuration["RABBITMQ_PASSWORD"] ?? "guest";
        _queueName = configuration["RABBITMQ_QUEUE"] ?? "indexer.events";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // PERMANENT FIX: Retry loop to handle RabbitMQ startup delay
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    _logger.LogInformation("Attempting to connect to RabbitMQ at {Host}:{Port}...", _hostName, _port);
                    InitializeConnection();
                }

                if (_channel != null)
                {
                    SubscribeToQueue();

                    // Stay alive while the connection is healthy
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("RabbitMQ not available yet. Retrying in 5 seconds... (Error: {Message})", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private void InitializeConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _hostName,
            Port = _port,
            UserName = _userName,
            Password = _password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true // Helps with network hiccups later
        };

        _connection = factory.CreateConnection("searchagent-subscriber");
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("Successfully connected and declared queue: {Queue}", _queueName);
    }

    private void SubscribeToQueue()
    {
        if (_channel == null) return;

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var indexingEvent = JsonSerializer.Deserialize<IndexingEvent>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Received RabbitMQ event: {EventType}", indexingEvent?.EventType ?? "Unknown");

                if (indexingEvent?.EventType == "IndexingCompleted")
                {
                    using var scope = _scopeFactory.CreateScope();

                    var featureManager = scope.ServiceProvider.GetRequiredService<IFeatureManager>();
                    if (!await featureManager.IsEnabledAsync(FeatureFlags.SearchAgent))
                    {
                        _logger.LogWarning("SearchAgent feature is disabled. Skipping agent run triggered by RabbitMQ event.");
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    var runner = scope.ServiceProvider.GetRequiredService<SearchAgentService.Services.SearchAgentService>();
                    await runner.RunAllAgentsAsync();
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while handling message. Requeuing...");
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("Subscribed to RabbitMQ queue. Waiting for events...");
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}