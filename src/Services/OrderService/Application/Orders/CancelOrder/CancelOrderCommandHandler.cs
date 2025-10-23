namespace OrderService.Application.Orders.CancelOrder;

using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IOrderRepository _repo;
    private readonly IMessageBus _bus;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        IOrderRepository repo,
        IMessageBus bus,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _repo = repo;
        _bus = bus;
        _logger = logger;
    }

    public async Task<bool> Handle(CancelOrderCommand req, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(req.OrderId, ct);
        
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found", req.OrderId);
            return false;
        }

        // Business Rule: Check if order can be cancelled
        if (!order.CanBeCancelled())
        {
            _logger.LogWarning("Order {OrderId} cannot be cancelled. Status: {Status}, CreatedAt: {CreatedAt}", 
                order.Id, order.Status, order.CreatedAtUtc);
            return false;
        }

        var reason = req.Reason ?? "Cancelled by customer";
        
        // Cancel the order
        order.Cancel(reason);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} cancelled. Reason: {Reason}", order.Id, reason);

        // Publish cancellation event for compensation (release stock, refund payment)
        await _bus.PublishAsync(new OrderCancelledEvent
        {
            OrderId = order.Id,
            Reason = reason,
            CorrelationId = order.Id.ToString()
        }, ct);

        // Release stock if it was reserved
        if (order.StockReservationId.HasValue)
        {
            await _bus.PublishAsync(new StockReleasedEvent
            {
                OrderId = order.Id,
                ReservationId = order.StockReservationId.Value,
                CorrelationId = order.Id.ToString()
            }, ct);
        }

        return true;
    }
}
