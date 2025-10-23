namespace BuildingBlocks.Messaging.Events;

public sealed record PaymentProcessedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public bool Success { get; init; }
    public string? FailureReason { get; init; }
    public decimal Amount { get; init; }
}