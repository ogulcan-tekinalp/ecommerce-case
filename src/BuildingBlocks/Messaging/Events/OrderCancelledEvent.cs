namespace BuildingBlocks.Messaging.Events;

public sealed record OrderCancelledEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime CancelledAtUtc { get; init; } = DateTime.UtcNow;
}