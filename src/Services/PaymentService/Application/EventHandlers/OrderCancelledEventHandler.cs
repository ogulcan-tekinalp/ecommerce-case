using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using PaymentService.Application.RefundPayment;
using PaymentService.Application.Abstractions;

namespace PaymentService.Application.EventHandlers;

public sealed class OrderCancelledEventHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageBus _bus;
    private readonly ILogger<OrderCancelledEventHandler> _logger;

    public OrderCancelledEventHandler(
        IServiceScopeFactory scopeFactory,
        IMessageBus bus,
        ILogger<OrderCancelledEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
        _logger = logger;

        // Subscribe to order cancelled events to trigger refunds when needed
        _bus.Subscribe<OrderCancelledEvent>(HandleAsync);
    }

    private async Task HandleAsync(OrderCancelledEvent evt)
    {
        _logger.LogInformation("🔔 Received OrderCancelledEvent for Order {OrderId}", evt.OrderId);

        using var scope = _scopeFactory.CreateScope();
        var paymentRepo = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        try
        {
            var payment = await paymentRepo.GetByOrderIdAsync(evt.OrderId);
            if (payment == null)
            {
                _logger.LogInformation("No payment found for Order {OrderId}, skipping refund", evt.OrderId);
                return;
            }

            if (payment.Status != PaymentStatus.Success)
            {
                _logger.LogInformation("Payment {PaymentId} for Order {OrderId} is not refundable (Status: {Status})", payment.Id, evt.OrderId, payment.Status);
                return;
            }

            // Trigger refund via mediator (uses existing RefundPaymentCommandHandler)
            var amount = payment.Amount;
            var command = new RefundPaymentCommand(payment.Id, amount, $"Auto-refund due to order cancellation: {evt.Reason}");
            var result = await mediator.Send(command);

            if (result.Success)
            {
                _logger.LogInformation("✅ Auto-refund processed for Payment {PaymentId} (Order {OrderId})", payment.Id, evt.OrderId);
            }
            else
            {
                _logger.LogWarning("❌ Auto-refund failed for Payment {PaymentId} (Order {OrderId}): {Reason}", payment.Id, evt.OrderId, result.FailureReason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling OrderCancelledEvent for Order {OrderId}", evt.OrderId);
        }
    }
}
