namespace OrderService.Application.Orders.RetryOrder;

using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Application.Sagas;
using OrderService.Domain.Enums;

public sealed class RetryOrderCommandHandler : IRequestHandler<RetryOrderCommand, bool>
{
    private readonly IOrderRepository _repo;
    private readonly OrderSaga _saga;
    private readonly ILogger<RetryOrderCommandHandler> _logger;

    public RetryOrderCommandHandler(
        IOrderRepository repo,
        OrderSaga saga,
        ILogger<RetryOrderCommandHandler> logger)
    {
        _repo = repo;
        _saga = saga;
        _logger = logger;
    }

    public async Task<bool> Handle(RetryOrderCommand req, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(req.OrderId, ct);
        
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found", req.OrderId);
            return false;
        }

        // Business Rule: Only cancelled orders can be retried
        if (order.Status != OrderStatus.Cancelled)
        {
            _logger.LogWarning("Order {OrderId} cannot be retried. Status: {Status}", 
                order.Id, order.Status);
            return false;
        }

        // Business Rule: Check if order is still within cancellation window (2 hours)
        // If more than 2 hours passed, probably too late to retry
        if (DateTime.UtcNow - order.CreatedAtUtc > TimeSpan.FromHours(2))
        {
            _logger.LogWarning("Order {OrderId} is too old to retry (created {CreatedAt})", 
                order.Id, order.CreatedAtUtc);
            return false;
        }

        _logger.LogInformation("Retrying Order {OrderId}. Previous reason: {Reason}", 
            order.Id, order.CancellationReason);

        // Reset order to Pending state
        order.Status = OrderStatus.Pending;
        order.CancelledAtUtc = null;
        order.CancellationReason = null;
        order.StockReservationId = null; // Clear previous reservation
        order.PaymentId = null; // Clear previous payment

        await _repo.SaveChangesAsync(ct);

        // Restart the saga
        _ = Task.Run(async () => await _saga.StartOrderFlowAsync(order.Id, ct), ct);

        _logger.LogInformation("🔄 Order {OrderId} retry initiated", order.Id);

        return true;
    }
}
