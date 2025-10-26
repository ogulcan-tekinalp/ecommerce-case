namespace InventoryService.Application.Inventory.ReserveStock;

using MediatR;

public sealed record ReserveStockCommand(
    Guid OrderId,
    List<ReserveStockItemDto> Items,
    Guid? CustomerId = null,
    bool IsVip = false
) : IRequest<ReserveStockResult>;

public record ReserveStockItemDto(Guid ProductId, int Quantity);

public record ReserveStockResult(
    bool Success,
    Guid? ReservationId,
    string? FailureReason
);