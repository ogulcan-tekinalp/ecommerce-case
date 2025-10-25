using MediatR;

namespace InventoryService.Application.Inventory.BulkUpdateStock;

public record BulkUpdateStockCommand(
    List<BulkUpdateStockItemDto> Items
) : IRequest<BulkUpdateStockResult>;

public record BulkUpdateStockItemDto(
    Guid ProductId,
    int QuantityChange,
    string Reason,
    bool IsAddition = true
);

public record BulkUpdateStockResult(
    bool Success,
    int UpdatedCount,
    List<string> Errors
);
