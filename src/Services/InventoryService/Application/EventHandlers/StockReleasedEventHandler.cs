namespace InventoryService.Application.EventHandlers;

using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using InventoryService.Application.Inventory.ReleaseStock;
using MediatR;

public sealed class StockReleasedEventHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageBus _bus;
    private readonly ILogger<StockReleasedEventHandler> _logger;

    public StockReleasedEventHandler(
        IServiceScopeFactory scopeFactory,
        IMessageBus bus, 
        ILogger<StockReleasedEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
        _logger = logger;

        _bus.Subscribe<StockReleasedEvent>(HandleAsync);
    }

    private async Task HandleAsync(StockReleasedEvent evt)
    {
        _logger.LogInformation("📦 [INVENTORY] Received StockReleasedEvent for Order {OrderId}, Reservation {ReservationId}",
            evt.OrderId, evt.ReservationId);

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        var command = new ReleaseStockCommand(evt.ReservationId, "Order cancelled or payment failed");
        var result = await mediator.Send(command);

        if (result)
        {
            _logger.LogInformation("✅ [INVENTORY] Stock released for Reservation {ReservationId}", evt.ReservationId);
        }
        else
        {
            _logger.LogWarning("❌ [INVENTORY] Failed to release stock for Reservation {ReservationId}", evt.ReservationId);
        }
    }
}