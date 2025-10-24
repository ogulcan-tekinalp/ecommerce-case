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

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        var command = new ProcessPaymentCommand(
            OrderId: evt.OrderId,
            CustomerId: Guid.Empty,
            Amount: 15000m,
            Method: PaymentMethod.CreditCard
        );

        var result = await mediator.Send(command);

        if (result.Success)
        {
            await _bus.PublishAsync(new PaymentProcessedEvent
            {
                OrderId = evt.OrderId,
                PaymentId = result.PaymentId!.Value,
                Success = true,
                TransactionId = result.TransactionId,
                CorrelationId = evt.CorrelationId
            });

            _logger.LogInformation("✅ [PAYMENT] Payment successful for Order {OrderId}, Transaction: {TransactionId}",
                evt.OrderId, result.TransactionId);
        }
        else
        {
            await _bus.PublishAsync(new PaymentProcessedEvent
            {
                OrderId = evt.OrderId,
                PaymentId = result.PaymentId ?? Guid.Empty,
                Success = false,
                FailureReason = result.FailureReason,
                CorrelationId = evt.CorrelationId
            });

            _logger.LogWarning("❌ [PAYMENT] Payment failed for Order {OrderId}: {Reason}",
                evt.OrderId, result.FailureReason);
        }
    }
}