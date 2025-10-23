namespace BuildingBlocks.Messaging.Events;

public sealed record StockReleasedEvent : IntegrationEvent
{
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }
}