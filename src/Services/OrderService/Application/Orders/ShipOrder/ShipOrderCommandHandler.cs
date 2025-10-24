namespace OrderService.Application.Orders.ShipOrder;

using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Domain.Enums;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;

public sealed class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, bool>
{
    private readonly IOrderRepository _repo;
    private readonly IMessageBus _bus;
    private readonly ILogger<ShipOrderCommandHandler> _logger;

    public ShipOrderCommandHandler(
        IOrderRepository repo,
        IMessageBus bus,
        ILogger<ShipOrderCommandHandler> logger)
    {
        _repo = repo;
        _bus = bus;
        _logger = logger;
    }

    public async Task<bool> Handle(ShipOrderCommand req, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(req.OrderId, ct);
        
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found", req.OrderId);
            return false;
        }

        // Business Rule: Only confirmed orders can be shipped
        if (order.Status != OrderStatus.Confirmed)
        {
            _logger.LogWarning("Order {OrderId} cannot be shipped. Status: {Status}", 
                order.Id, order.Status);
            return false;
        }

        // Ship the order
        order.Status = OrderStatus.Shipped;
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("📦 Order {OrderId} shipped with tracking {TrackingNumber}", 
            order.Id, req.TrackingNumber);

        // Publish shipping event
        await _bus.PublishAsync(new OrderShippedEvent
        {
            OrderId = order.Id,
            TrackingNumber = req.TrackingNumber,
            Carrier = req.Carrier,
            CorrelationId = order.Id.ToString()
        }, ct);

        return true;
    }
}