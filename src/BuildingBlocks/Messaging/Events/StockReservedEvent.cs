namespace BuildingBlocks.Messaging.Events;

public sealed record StockReservedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }
    public bool Success { get; init; }
    public bool IsVip { get; init; }
    public string? FailureReason { get; init; }
}