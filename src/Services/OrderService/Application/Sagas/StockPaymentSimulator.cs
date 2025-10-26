namespace OrderService.Application.Sagas;

using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;

/// <summary>
/// Temporary simulator for testing order flow.
/// Simulates InventoryService and PaymentService responses.
/// Remove this when real services are implemented.
/// </summary>
public sealed class StockPaymentSimulator
{
    private readonly IMessageBus _bus;
    private readonly ILogger<StockPaymentSimulator> _logger;
    private readonly Random _random = new();

    public StockPaymentSimulator(IMessageBus bus, ILogger<StockPaymentSimulator> logger)
    {
        _bus = bus;
        _logger = logger;
    }
    
    public void Initialize()
    {
        // Subscribe to events and simulate responses
        _bus.Subscribe<OrderCreatedEvent>(SimulateStockReservationAsync);
        _logger.LogInformation("📥 [SIMULATOR] Subscribed to OrderCreatedEvent");
    }

    private async Task SimulateStockReservationAsync(OrderCreatedEvent evt)
    {
        // Simulate processing delay
        await Task.Delay(500);

        // 90% success rate for stock reservation
        var stockAvailable = _random.Next(100) < 90;

        if (stockAvailable)
        {
            _logger.LogInformation("✅ [SIMULATOR] Stock reserved for Order {OrderId}", evt.OrderId);
            
            await _bus.PublishAsync(new StockReservedEvent
            {
                OrderId = evt.OrderId,
                ReservationId = Guid.NewGuid(),
                Success = true,
                CorrelationId = evt.CorrelationId
            });

            // Simulate payment processing after stock reservation
            await Task.Delay(500);
            await SimulatePaymentAsync(evt);
        }
        else
        {
            _logger.LogWarning("❌ [SIMULATOR] Stock unavailable for Order {OrderId}", evt.OrderId);
            
            await _bus.PublishAsync(new StockReservedEvent
            {
                OrderId = evt.OrderId,
                ReservationId = Guid.NewGuid(),
                Success = false,
                FailureReason = "Insufficient stock",
                CorrelationId = evt.CorrelationId
            });
        }
    }

    private async Task SimulatePaymentAsync(OrderCreatedEvent evt)
    {
        // 85% success, 10% timeout (eventual success), 5% failure
        var outcome = _random.Next(100);

        if (outcome < 85)
        {
            // Success
            _logger.LogInformation("✅ [SIMULATOR] Payment successful for Order {OrderId}", evt.OrderId);
            
            await _bus.PublishAsync(new PaymentProcessedEvent
            {
                OrderId = evt.OrderId,
                PaymentId = Guid.NewGuid(),
                Amount = evt.TotalAmount,
                Success = true,
                CorrelationId = evt.CorrelationId
            });
        }
        else if (outcome < 95)
        {
            // Timeout - retry after delay
            _logger.LogWarning("⏳ [SIMULATOR] Payment timeout for Order {OrderId}, retrying...", evt.OrderId);
            await Task.Delay(2000);
            
            // Retry succeeds
            await _bus.PublishAsync(new PaymentProcessedEvent
            {
                OrderId = evt.OrderId,
                PaymentId = Guid.NewGuid(),
                Amount = evt.TotalAmount,
                Success = true,
                CorrelationId = evt.CorrelationId
            });
        }
        else
        {
            // Failure
            _logger.LogError("❌ [SIMULATOR] Payment failed for Order {OrderId}", evt.OrderId);
            
            await _bus.PublishAsync(new PaymentProcessedEvent
            {
                OrderId = evt.OrderId,
                PaymentId = Guid.NewGuid(),
                Amount = evt.TotalAmount,
                Success = false,
                FailureReason = "Insufficient funds",
                CorrelationId = evt.CorrelationId
            });
        }
    }
}