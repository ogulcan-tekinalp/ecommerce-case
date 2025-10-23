namespace BuildingBlocks.Messaging.Events;

public sealed record OrderConfirmedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public DateTime ConfirmedAtUtc { get; init; } = DateTime.UtcNow;
}