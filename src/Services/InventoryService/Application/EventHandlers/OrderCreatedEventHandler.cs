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
        if (evt.IsVip)
        {
            _logger.LogInformation("⭐ [INVENTORY] Received VIP OrderCreatedEvent for Order {OrderId} - PRIORITY PROCESSING", evt.OrderId);
            await ProcessVipStockReservationAsync(evt);
        }
        else
        {
            _logger.LogInformation("📦 [INVENTORY] Received OrderCreatedEvent for Order {OrderId}", evt.OrderId);
            await ProcessRegularStockReservationAsync(evt);
        }
    }

    private async Task ProcessVipStockReservationAsync(OrderCreatedEvent evt)
    {
        // VIP orders get immediate processing without delays
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        var command = new ReserveStockCommand(
            evt.OrderId,
            evt.Items.Select(i => new ReserveStockItemDto(i.ProductId, (int)i.Quantity)).ToList(),
            CustomerId: evt.CustomerId,
            IsVip: true // VIP flag for priority processing
        );

        var result = await mediator.Send(command);

        await _bus.PublishAsync(new StockReservedEvent
        {
            OrderId = evt.OrderId,
            ReservationId = result.ReservationId ?? Guid.Empty,
            Success = result.Success,
            IsVip = true,
            FailureReason = result.FailureReason,
            CorrelationId = evt.CorrelationId
        });

        if (result.Success)
        {
            _logger.LogInformation("⚡ [INVENTORY] VIP stock reserved for Order {OrderId}, Reservation {ReservationId}",
                evt.OrderId, result.ReservationId);
        }
        else
        {
            _logger.LogWarning("❌ [INVENTORY] VIP stock reservation failed for Order {OrderId}: {Reason}",
                evt.OrderId, result.FailureReason);
        }
    }

    private async Task ProcessRegularStockReservationAsync(OrderCreatedEvent evt)
    {
        // Regular orders processed normally without artificial delays
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        var command = new ReserveStockCommand(
            evt.OrderId,
            evt.Items.Select(i => new ReserveStockItemDto(i.ProductId, (int)i.Quantity)).ToList(),
            CustomerId: evt.CustomerId,
            IsVip: false
        );

        var result = await mediator.Send(command);

        await _bus.PublishAsync(new StockReservedEvent
        {
            OrderId = evt.OrderId,
            ReservationId = result.ReservationId ?? Guid.Empty,
            Success = result.Success,
            IsVip = false,
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