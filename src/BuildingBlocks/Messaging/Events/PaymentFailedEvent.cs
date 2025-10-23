namespace BuildingBlocks.Messaging.Events;

public sealed record PaymentFailedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public string Reason { get; init; } = string.Empty;
}