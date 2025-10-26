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

        if (evt.IsVip)
        {
            _logger.LogInformation("⭐ [PAYMENT] Processing VIP payment for Order {OrderId} - PRIORITY", evt.OrderId);
            await ProcessVipPaymentAsync(evt);
        }
        else
        {
            _logger.LogInformation("💳 [PAYMENT] Processing regular payment for Order {OrderId}", evt.OrderId);
            await ProcessRegularPaymentAsync(evt);
        }
    }

    private async Task ProcessVipPaymentAsync(StockReservedEvent evt)
    {
        // VIP payments get faster processing with higher success rate
        await Task.Delay(500); // Faster processing for VIP
        
        // VIP Payment simulation: 90% success, 8% timeout, 2% failure (better rates)
        var random = new Random();
        var outcome = random.Next(100);
        
        if (outcome < 90)
        {
            // VIP Success (90%)
            var transactionId = Guid.NewGuid().ToString("N")[..16].ToUpper();
            
            await _bus.PublishAsync(new PaymentProcessedEvent
            {
                OrderId = evt.OrderId,
                PaymentId = Guid.NewGuid(),
                Success = true,
                TransactionId = transactionId,
                CorrelationId = evt.CorrelationId
            });

            _logger.LogInformation("✅ [PAYMENT] VIP payment successful for Order {OrderId}, Transaction: {TransactionId}",
                evt.OrderId, transactionId);
        }
        else if (outcome < 98)
        {
            // VIP Timeout - retry after delay (8%)
            _logger.LogWarning("⏳ [PAYMENT] VIP payment timeout for Order {OrderId}, retrying...", evt.OrderId);
            await Task.Delay(1000); // Faster retry for VIP
            
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
            
            _logger.LogInformation("✅ [PAYMENT] VIP payment successful after retry for Order {OrderId}, Transaction: {TransactionId}",
                evt.OrderId, transactionId);
        }
        else
        {
            // VIP Failure (2%)
            await _bus.PublishAsync(new PaymentProcessedEvent
            {
                OrderId = evt.OrderId,
                PaymentId = Guid.NewGuid(),
                Success = false,
                FailureReason = "Insufficient funds",
                CorrelationId = evt.CorrelationId
            });

            _logger.LogWarning("❌ [PAYMENT] VIP payment failed for Order {OrderId}: Insufficient funds",
                evt.OrderId);
        }
    }

    private async Task ProcessRegularPaymentAsync(StockReservedEvent evt)
    {
        // Regular payment processing with standard delays
        await Task.Delay(1000); // Standard processing time
        
        // Regular Payment simulation: 85% success, 10% timeout, 5% failure
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