namespace OrderService.Application.Sagas;

using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Abstractions;

public class OrderSaga
{
    private readonly IMessageBus _bus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderSaga> _logger;

    public OrderSaga(IMessageBus bus, IServiceScopeFactory scopeFactory, ILogger<OrderSaga> logger)
    {
        _bus = bus;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _bus.Subscribe<StockReservedEvent>(HandleStockReservedAsync);
        _bus.Subscribe<PaymentProcessedEvent>(HandlePaymentProcessedAsync);
        _bus.Subscribe<PaymentFailedEvent>(HandlePaymentFailedAsync);
    }

    public virtual async Task StartOrderFlowAsync(Guid orderId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting order flow for Order {OrderId}", orderId);

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = await repo.GetByIdAsync(orderId, ct);
        if (order is null)
        {
            _logger.LogError("Order {OrderId} not found", orderId);
            return;
        }

        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            CorrelationId = orderId.ToString(),
            Items = order.Items.Select(i => new OrderItemDto(
                i.ProductId,
                i.Quantity,
                i.UnitPrice
            )).ToList()
        };

        await _bus.PublishAsync(orderCreatedEvent, ct);
        _logger.LogInformation("Published OrderCreatedEvent for Order {OrderId}", orderId);
    }

    private async Task HandleStockReservedAsync(StockReservedEvent evt)
    {
        _logger.LogInformation("Handling StockReservedEvent for Order {OrderId}, Success: {Success}",
            evt.OrderId, evt.Success);

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = await repo.GetByIdAsync(evt.OrderId);
        if (order is null) return;

        if (!evt.Success)
        {
            _logger.LogWarning("Stock reservation failed for Order {OrderId}: {Reason}",
                evt.OrderId, evt.FailureReason);
            
            order.Cancel($"Stock reservation failed: {evt.FailureReason}");
            await repo.SaveChangesAsync();

            await _bus.PublishAsync(new OrderCancelledEvent
            {
                OrderId = order.Id,
                Reason = order.CancellationReason ?? "Stock unavailable",
                CorrelationId = evt.CorrelationId
            });
            return;
        }

        // Stock reserved successfully - save reservation ID and wait for payment
        order.StockReservationId = evt.ReservationId;
        await repo.SaveChangesAsync();
        
        _logger.LogInformation("Stock reserved for Order {OrderId}, waiting for payment...", evt.OrderId);
    }

    private async Task HandlePaymentProcessedAsync(PaymentProcessedEvent evt)
    {
        _logger.LogInformation("Handling PaymentProcessedEvent for Order {OrderId}, Success: {Success}",
            evt.OrderId, evt.Success);

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = await repo.GetByIdAsync(evt.OrderId);
        if (order is null) return;

        if (!evt.Success)
        {
            _logger.LogWarning("Payment failed for Order {OrderId}: {Reason}",
                evt.OrderId, evt.FailureReason);

            await CompensateFailedOrderAsync(order, $"Payment failed: {evt.FailureReason}", repo);
            return;
        }

        // Payment successful - confirm order!
        order.PaymentId = evt.PaymentId;
        order.Confirm();
        await repo.SaveChangesAsync();

        await _bus.PublishAsync(new OrderConfirmedEvent
        {
            OrderId = order.Id,
            CorrelationId = evt.CorrelationId
        });

        _logger.LogInformation("✅ Order {OrderId} confirmed successfully!", evt.OrderId);
    }

    private async Task HandlePaymentFailedAsync(PaymentFailedEvent evt)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var order = await repo.GetByIdAsync(evt.OrderId);
        if (order is null) return;

        await CompensateFailedOrderAsync(order, evt.Reason, repo);
    }

    private async Task CompensateFailedOrderAsync(Domain.Entities.Order order, string reason, IOrderRepository repo)
    {
        _logger.LogWarning("Compensating failed order {OrderId}: {Reason}", order.Id, reason);

        if (order.StockReservationId.HasValue)
        {
            await _bus.PublishAsync(new StockReleasedEvent
            {
                OrderId = order.Id,
                ReservationId = order.StockReservationId.Value,
                CorrelationId = order.Id.ToString()
            });
        }

        order.Cancel(reason);
        await repo.SaveChangesAsync();

        await _bus.PublishAsync(new OrderCancelledEvent
        {
            OrderId = order.Id,
            Reason = reason,
            CorrelationId = order.Id.ToString()
        });
    }
}