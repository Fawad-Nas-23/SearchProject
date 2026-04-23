using System;
using System.Text;
using System.Text.Json;
using indexer.Messaging;
using NLog;
using RabbitMQ.Client;
using Indexer;

namespace indexer.Messaging;

public class RabbitMQPublisher : IDisposable
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly string _queueName;
    private readonly bool _isConnected;

    public RabbitMQPublisher()
    {
        _queueName = Config.RABBITMQ_QUEUE;

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = Config.RABBITMQ_HOST,
                Port = Config.RABBITMQ_PORT,
                UserName = Config.RABBITMQ_USER,
                Password = Config.RABBITMQ_PASSWORD,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
                SocketReadTimeout = TimeSpan.FromSeconds(5),
                SocketWriteTimeout = TimeSpan.FromSeconds(5)
            };

            _connection = factory.CreateConnection("indexer-publisher");
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _isConnected = true;
            _logger.Info("Connected to RabbitMQ at {Host}:{Port}, queue '{Queue}'",
                Config.RABBITMQ_HOST, Config.RABBITMQ_PORT, _queueName);
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger.Warn(ex, "Could not connect to RabbitMQ - notification will be skipped");
        }
    }

    public void Publish(IndexingEvent evt)
    {
        if (!_isConnected || _channel is null)
        {
            _logger.Warn("Not connected to RabbitMQ - skipping publish");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(evt);
            var body = Encoding.UTF8.GetBytes(json);

            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";
            props.MessageId = Guid.NewGuid().ToString();
            props.Type = evt.EventType;
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: "",
                routingKey: _queueName,
                basicProperties: props,
                body: body);

            _logger.Info("Published '{EventType}' ({Bytes} bytes) to queue '{Queue}'",
                evt.EventType, body.Length, _queueName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error publishing to RabbitMQ");
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch { /* ignore shutdown errors */ }
    }
}