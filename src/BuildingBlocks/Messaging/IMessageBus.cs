namespace BuildingBlocks.Messaging;

using BuildingBlocks.Messaging.Events;

public interface IMessageBus
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : IntegrationEvent;
    void Subscribe<T>(Func<T, Task> handler) where T : IntegrationEvent;
}