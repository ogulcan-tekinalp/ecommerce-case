namespace OrderService.Application.Orders.ShipOrder;

using MediatR;

public sealed record ShipOrderCommand(
    Guid OrderId,
    string TrackingNumber,
    string Carrier = "DHL"
) : IRequest<bool>;