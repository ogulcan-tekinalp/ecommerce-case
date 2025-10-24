using MediatR;

namespace InventoryService.Application.Inventory.GetProductStock;

public record GetProductStockQuery(Guid ProductId) : IRequest<GetProductStockResult?>;

public record GetProductStockResult(
    Guid ProductId,
    string Name,
    int AvailableQuantity,
    int ReservedQuantity,
    decimal Price
);