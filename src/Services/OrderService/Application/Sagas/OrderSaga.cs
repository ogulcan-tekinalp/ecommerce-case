namespace OrderService.Application.Sagas;

using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;

public sealed class OrderSaga
{
    private readonly IMessageBus _bus;
    private readonly IOrderRepository _repo;
    private readonly ILogger<OrderSaga> _logger;

    public OrderSaga(IMessageBus bus, IOrderRepository repo, ILogger<OrderSaga> logger)
    {
        _bus = bus;
        _repo = repo;
        _logger = logger;

        // Subscribe to events
        _bus.Subscribe<StockReservedEvent>(HandleStockReservedAsync);
        _bus.Subscribe<PaymentProcessedEvent>(HandlePaymentProcessedAsync);
        _bus.Subscribe<PaymentFailedEvent>(HandlePaymentFailedAsync);
    }

    public async Task StartOrderFlowAsync(Guid orderId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting order flow for Order {OrderId}", orderId);

        var order = await _repo.GetByIdAsync(orderId, ct);
        if (order is null)
        {
            _logger.LogError("Order {OrderId} not found", orderId);
            return;
        }

        // Step 1: Publish OrderCreatedEvent to trigger stock reservation
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

        var order = await _repo.GetByIdAsync(evt.OrderId);
        if (order is null) return;

        if (!evt.Success)
        {
            // Stock reservation failed - cancel order
            _logger.LogWarning("Stock reservation failed for Order {OrderId}: {Reason}",
                evt.OrderId, evt.FailureReason);
            
            order.Cancel($"Stock reservation failed: {evt.FailureReason}");
            await _repo.SaveChangesAsync();

            await _bus.PublishAsync(new OrderCancelledEvent
            {
                OrderId = order.Id,
                Reason = order.CancellationReason ?? "Stock unavailable",
                CorrelationId = evt.CorrelationId
            });
            return;
        }

        // Stock reserved successfully - proceed to payment
        order.StockReservationId = evt.ReservationId;
        await _repo.SaveChangesAsync();

        var paymentEvent = new PaymentProcessedEvent
        {
            OrderId = order.Id,
            PaymentId = Guid.NewGuid(),
            Amount = order.TotalAmount,
            CorrelationId = evt.CorrelationId
        };

        // In real scenario, this would be sent to PaymentService
        // For now, we'll simulate payment processing here
        await _bus.PublishAsync(paymentEvent);
        _logger.LogInformation("Triggered payment processing for Order {OrderId}", evt.OrderId);
    }

    private async Task HandlePaymentProcessedAsync(PaymentProcessedEvent evt)
    {
        _logger.LogInformation("Handling PaymentProcessedEvent for Order {OrderId}, Success: {Success}",
            evt.OrderId, evt.Success);

        var order = await _repo.GetByIdAsync(evt.OrderId);
        if (order is null) return;

        if (!evt.Success)
        {
            // Payment failed - compensate: release stock
            _logger.LogWarning("Payment failed for Order {OrderId}: {Reason}",
                evt.OrderId, evt.FailureReason);

            await CompensateFailedOrderAsync(order, $"Payment failed: {evt.FailureReason}");
            return;
        }

        // Payment successful - confirm order!
        order.PaymentId = evt.PaymentId;
        order.Confirm();
        await _repo.SaveChangesAsync();

        await _bus.PublishAsync(new OrderConfirmedEvent
        {
            OrderId = order.Id,
            CorrelationId = evt.CorrelationId
        });

        _logger.LogInformation("✅ Order {OrderId} confirmed successfully!", evt.OrderId);
    }

    private async Task HandlePaymentFailedAsync(PaymentFailedEvent evt)
    {
        var order = await _repo.GetByIdAsync(evt.OrderId);
        if (order is null) return;

        await CompensateFailedOrderAsync(order, evt.Reason);
    }

    private async Task CompensateFailedOrderAsync(Domain.Entities.Order order, string reason)
    {
        _logger.LogWarning("Compensating failed order {OrderId}: {Reason}", order.Id, reason);

        // Release stock reservation
        if (order.StockReservationId.HasValue)
        {
            await _bus.PublishAsync(new StockReleasedEvent
            {
                OrderId = order.Id,
                ReservationId = order.StockReservationId.Value,
                CorrelationId = order.Id.ToString()
            });
        }

        // Cancel order
        order.Cancel(reason);
        await _repo.SaveChangesAsync();

        await _bus.PublishAsync(new OrderCancelledEvent
        {
            OrderId = order.Id,
            Reason = reason,
            CorrelationId = order.Id.ToString()
        });
    }
}