using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.ProcessPayment;
using MediatR;

namespace PaymentService.Application.EventHandlers;

public sealed class StockReservedEventHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageBus _bus;
    private readonly ILogger<StockReservedEventHandler> _logger;

    public StockReservedEventHandler(
        IServiceScopeFactory scopeFactory,
        IMessageBus bus,
        ILogger<StockReservedEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
        _logger = logger;

        _bus.Subscribe<StockReservedEvent>(HandleAsync);
    }

    private async Task HandleAsync(StockReservedEvent evt)
    {
        if (!evt.Success)
        {
            _logger.LogInformation("⏭️ [PAYMENT] Skipping payment - stock reservation failed for Order {OrderId}", evt.OrderId);
            return;
        }

        _logger.LogInformation("💳 [PAYMENT] Processing payment for Order {OrderId}", evt.OrderId);

        // Payment simulation: 85% success, 10% timeout, 5% failure
        await Task.Delay(1000); // Simulate processing time
        
        var random = new Random();
        var outcome = random.Next(100);
        
        if (outcome < 85)
        {
            // Success (85%)
            var transactionId = Guid.NewGuid().ToString("N")[..16].ToUpper();
            
            await _bus.PublishAsync(new PaymentProcessedEvent
            {
                OrderId = evt.OrderId,
                PaymentId = Guid.NewGuid(),
                Success = true,
                TransactionId = transactionId,
                CorrelationId = evt.CorrelationId
            });

            _logger.LogInformation("✅ [PAYMENT] Payment successful for Order {OrderId}, Transaction: {TransactionId}",
                evt.OrderId, transactionId);
        }
        else if (outcome < 95)
        {
            // Timeout - retry after delay (10%)
            _logger.LogWarning("⏳ [PAYMENT] Payment timeout for Order {OrderId}, retrying...", evt.OrderId);
            await Task.Delay(2000);
            
            // Retry succeeds
            var transactionId = Guid.NewGuid().ToString("N")[..16].ToUpper();
            
            await _bus.PublishAsync(new PaymentProcessedEvent
            {
                OrderId = evt.OrderId,
                PaymentId = Guid.NewGuid(),
                Success = true,
                TransactionId = transactionId,
                CorrelationId = evt.CorrelationId
            });
            
            _logger.LogInformation("✅ [PAYMENT] Payment successful after retry for Order {OrderId}, Transaction: {TransactionId}",
                evt.OrderId, transactionId);
        }
        else
        {
            // Failure (5%)
            await _bus.PublishAsync(new PaymentProcessedEvent
            {
                OrderId = evt.OrderId,
                PaymentId = Guid.NewGuid(),
                Success = false,
                FailureReason = "Insufficient funds",
                CorrelationId = evt.CorrelationId
            });

            _logger.LogWarning("❌ [PAYMENT] Payment failed for Order {OrderId}: Insufficient funds",
                evt.OrderId);
        }
    }
}