using System.Text;
using System.Text.Json;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using BuildingBlocks.Observability;
using System.Collections.Generic;
using System.Linq;

namespace BuildingBlocks.Messaging;

public sealed class RabbitMqMessageBus : IMessageBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqMessageBus> _logger;
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly string _exchangeName = "ecommerce.events";
    private readonly int _maxRetries = 3;

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
            var dlqQueueName = $"dlq.{eventName}";

            // Declare main queue and DLQ
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(dlqQueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queueName, _exchangeName, eventName);

            // Start consuming
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                try
                {
                    var json = Encoding.UTF8.GetString(body);
                    var @event = JsonSerializer.Deserialize<TEvent>(json);

                    if (@event != null)
                    {
                        // Ensure correlation id is present
                        if (string.IsNullOrEmpty(@event.CorrelationId))
                        {
                            try
                            {
                                @event = @event with { CorrelationId = CorrelationContext.Id };
                            }
                            catch
                            {
                                // ignore
                            }
                        }

                        await InvokeHandlersAsync(@event);
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                    else
                    {
                        _logger.LogWarning("Deserialized event {EventName} was null", eventName);
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event {EventName}", eventName);

                    // Read retry count header
                    var props = ea.BasicProperties;
                    var headers = props?.Headers;
                    int retryCount = 0;

                    if (headers != null && headers.TryGetValue("x-retry-count", out var raw))
                    {
                        retryCount = ParseRetryHeader(raw);
                    }

                    if (retryCount < _maxRetries)
                    {
                        // Republish with incremented retry header
                        var newProps = _channel.CreateBasicProperties();
                        newProps.Persistent = true;
                        newProps.ContentType = props?.ContentType ?? "application/json";
                        newProps.Headers = new Dictionary<string, object>();

                        // copy existing headers
                        if (props?.Headers != null)
                        {
                            foreach (var kv in props.Headers)
                            {
                                newProps.Headers[kv.Key] = kv.Value;
                            }
                        }

                        var nextCount = retryCount + 1;
                        newProps.Headers["x-retry-count"] = Encoding.UTF8.GetBytes(nextCount.ToString());

                        _channel.BasicPublish(_exchangeName, eventName, newProps, body);
                        _logger.LogWarning("Requeued {EventName} (retry {Retry})", eventName, nextCount);
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                    else
                    {
                        // Move to DLQ
                        var dlqQueueNameLocal = $"dlq.{eventName}";
                        var dlqProps = _channel.CreateBasicProperties();
                        dlqProps.Persistent = true;
                        dlqProps.ContentType = props?.ContentType ?? "application/json";
                        dlqProps.Headers = new Dictionary<string, object>();

                        if (props?.Headers != null)
                        {
                            foreach (var kv in props.Headers)
                            {
                                dlqProps.Headers[kv.Key] = kv.Value;
                            }
                        }

                        dlqProps.Headers["x-original-routing-key"] = eventName;

                        // Publish to DLQ queue (default exchange)
                        _channel.BasicPublish(exchange: "", routingKey: dlqQueueNameLocal, basicProperties: dlqProps, body: body);
                        _logger.LogWarning("Moved {EventName} to DLQ {DlqQueue}", eventName, dlqQueueNameLocal);
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
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

    private int ParseRetryHeader(object raw)
    {
        try
        {
            if (raw is byte[] bytes)
            {
                var s = Encoding.UTF8.GetString(bytes);
                if (int.TryParse(s, out var v)) return v;

                if (bytes.Length == 4)
                {
                    return BitConverter.ToInt32(bytes, 0);
                }
            }

            if (raw is long l) return (int)l;
            if (raw is int i) return i;
            if (raw is string str && int.TryParse(str, out var parsed)) return parsed;
        }
        catch
        {
            // ignore parse errors
        }

        return 0;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _logger.LogInformation("🐰 RabbitMQ connection closed");
    }
}