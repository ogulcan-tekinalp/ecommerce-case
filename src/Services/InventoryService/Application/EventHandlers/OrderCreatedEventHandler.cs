namespace InventoryService.Application.EventHandlers;

using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using InventoryService.Application.Inventory.ReserveStock;
using MediatR;

public sealed class OrderCreatedEventHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageBus _bus;
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(
        IServiceScopeFactory scopeFactory,
        IMessageBus bus, 
        ILogger<OrderCreatedEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
        _logger = logger;

        _bus.Subscribe<OrderCreatedEvent>(HandleAsync);
    }

    private async Task HandleAsync(OrderCreatedEvent evt)
    {
        _logger.LogInformation("📦 [INVENTORY] Received OrderCreatedEvent for Order {OrderId}", evt.OrderId);

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        var command = new ReserveStockCommand(
            evt.OrderId,
            evt.Items.Select(i => new ReserveStockItemDto(i.ProductId, (int)i.Quantity)).ToList()
        );

        var result = await mediator.Send(command);

        await _bus.PublishAsync(new StockReservedEvent
        {
            OrderId = evt.OrderId,
            ReservationId = result.ReservationId ?? Guid.Empty,
            Success = result.Success,
            FailureReason = result.FailureReason,
            CorrelationId = evt.CorrelationId
        });

        if (result.Success)
        {
            _logger.LogInformation("✅ [INVENTORY] Stock reserved for Order {OrderId}, Reservation {ReservationId}",
                evt.OrderId, result.ReservationId);
        }
        else
        {
            _logger.LogWarning("❌ [INVENTORY] Stock reservation failed for Order {OrderId}: {Reason}",
                evt.OrderId, result.FailureReason);
        }
    }
}