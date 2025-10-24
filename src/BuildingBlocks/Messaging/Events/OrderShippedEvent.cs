namespace BuildingBlocks.Messaging.Events;

public sealed record OrderShippedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string Carrier { get; init; } = string.Empty;
    public DateTime ShippedAtUtc { get; init; } = DateTime.UtcNow;
}