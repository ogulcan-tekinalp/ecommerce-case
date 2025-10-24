namespace InventoryService.Application.EventHandlers;

using BuildingBlocks.Messaging;
using BuildingBlocks.Messaging.Events;
using Microsoft.Extensions.Logging;
using InventoryService.Application.Inventory.ReleaseStock;
using MediatR;

public sealed class StockReleasedEventHandler
{
    private readonly ISender _mediator;
    private readonly IMessageBus _bus;
    private readonly ILogger<StockReleasedEventHandler> _logger;

    public StockReleasedEventHandler(ISender mediator, IMessageBus bus, ILogger<StockReleasedEventHandler> logger)
    {
        _mediator = mediator;
        _bus = bus;
        _logger = logger;

        // Subscribe to StockReleasedEvent
        _bus.Subscribe<StockReleasedEvent>(HandleAsync);
    }

    private async Task HandleAsync(StockReleasedEvent evt)
    {
        _logger.LogInformation("📦 [INVENTORY] Received StockReleasedEvent for Order {OrderId}, Reservation {ReservationId}",
            evt.OrderId, evt.ReservationId);

        var command = new ReleaseStockCommand(evt.ReservationId, "Order cancelled or payment failed");
        var result = await _mediator.Send(command);

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