namespace BuildingBlocks.Messaging;

using BuildingBlocks.Messaging.Events;
using BuildingBlocks.Observability;
using System.Collections.Concurrent;

public sealed class InMemoryMessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : IntegrationEvent
    {
        // Ensure correlation id is set from request context if not provided
        if (string.IsNullOrEmpty(message.CorrelationId))
        {
            try
            {
                message = message with { CorrelationId = BuildingBlocks.Observability.CorrelationContext.Id };
            }
            catch
            {
                // ignore immutability issues if any
            }
        }

        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var handlers))
            return Task.CompletedTask;

        var tasks = handlers
            .Cast<Func<T, Task>>()
            .Select(h => h(message));

        return Task.WhenAll(tasks);
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : IntegrationEvent
    {
        var type = typeof(T);
        _handlers.AddOrUpdate(
            type,
            _ => new List<Delegate> { handler },
            (_, existing) =>
            {
                existing.Add(handler);
                return existing;
            });
    }
}