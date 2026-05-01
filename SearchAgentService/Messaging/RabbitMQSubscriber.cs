using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SearchAgentService.Services;

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

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                Port = _port,
                UserName = _userName,
                Password = _password,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection("searchagent-subscriber");
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false);

            _logger.LogInformation(
                "SearchAgent subscriber connected to RabbitMQ at {Host}:{Port}, queue '{Queue}'",
                _hostName,
                _port,
                _queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Could not connect SearchAgent subscriber to RabbitMQ at {Host}:{Port}, queue '{Queue}'",
                _hostName,
                _port,
                _queueName);
        }

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel == null)
        {
            _logger.LogWarning("RabbitMQ channel is null. Subscriber not started.");
            return Task.CompletedTask;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                var indexingEvent = JsonSerializer.Deserialize<IndexingEvent>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                _logger.LogInformation(
                    "Received RabbitMQ event: {EventType}",
                    indexingEvent?.EventType ?? "Unknown");

                if (indexingEvent?.EventType == "IndexingCompleted")
                {
                    using var scope = _scopeFactory.CreateScope();
                    var runner = scope.ServiceProvider.GetRequiredService<SearchAgentRunner>();

                    var results = await runner.RunAllAgentsAsync();

                    _logger.LogInformation(
                        "Search agents executed after indexing event. Checked agents: {Count}",
                        results.Count);
                }
                else
                {
                    _logger.LogWarning(
                        "Ignored RabbitMQ event with type: {EventType}",
                        indexingEvent?.EventType ?? "Unknown");
                }

                _channel.BasicAck(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while handling RabbitMQ message");

                _channel.BasicNack(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ subscriber is now listening on queue '{Queue}'", _queueName);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();

            _connection?.Close();
            _connection?.Dispose();
        }
        catch
        {
            // Ignore shutdown errors
        }

        base.Dispose();
    }
}
