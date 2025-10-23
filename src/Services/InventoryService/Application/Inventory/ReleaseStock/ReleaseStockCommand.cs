namespace InventoryService.Application.Inventory.ReleaseStock;

using MediatR;

public sealed record ReleaseStockCommand(
    Guid ReservationId,
    string Reason
) : IRequest<bool>;