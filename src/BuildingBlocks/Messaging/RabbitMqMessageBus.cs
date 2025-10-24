using System.Text;
using System.Text.Json;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BuildingBlocks.Messaging;

public sealed class RabbitMqMessageBus : IMessageBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqMessageBus> _logger;
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly string _exchangeName = "ecommerce.events";

    public RabbitMqMessageBus(string connectionString, ILogger<RabbitMqMessageBus> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange (topic type for event routing)
        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

        _logger.LogInformation("🐰 RabbitMQ connection established to {Host}", factory.HostName);
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IntegrationEvent
    {
        var eventType = typeof(TEvent);
        var eventName = eventType.Name;

        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<Delegate>();

            // Create queue for this event type
            var queueName = $"queue.{eventName}";
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queueName, _exchangeName, eventName);

            // Start consuming
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var @event = JsonSerializer.Deserialize<TEvent>(json);

                    if (@event != null)
                    {
                        await InvokeHandlersAsync(@event);
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event {EventName}", eventName);
                    _channel.BasicNack(ea.DeliveryTag, false, requeue: true);
                }
            };

            _channel.BasicConsume(queueName, autoAck: false, consumer);
            _logger.LogInformation("📥 Subscribed to {EventName}", eventName);
        }

        _handlers[eventType].Add(handler);
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IntegrationEvent
    {
        var eventName = typeof(TEvent).Name;
        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.CorrelationId = @event.CorrelationId;

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: eventName,
            basicProperties: properties,
            body: body
        );

        _logger.LogInformation("📤 Published {EventName} with CorrelationId {CorrelationId}", 
            eventName, @event.CorrelationId);

        return Task.CompletedTask;
    }

    private async Task InvokeHandlersAsync<TEvent>(TEvent @event) where TEvent : IntegrationEvent
    {
        var eventType = typeof(TEvent);
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            foreach (var handler in handlers)
            {
                if (handler is Func<TEvent, Task> typedHandler)
                {
                    await typedHandler(@event);
                }
            }
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _logger.LogInformation("🐰 RabbitMQ connection closed");
    }
}