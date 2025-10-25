using MediatR;

namespace InventoryService.Application.Inventory.ValidateFlashSale;

public record ValidateFlashSaleCommand(
    Guid CustomerId,
    Guid ProductId,
    int RequestedQuantity
) : IRequest<ValidateFlashSaleResult>;

public record ValidateFlashSaleResult(
    bool IsValid,
    bool IsFlashSale,
    int MaxAllowedQuantity,
    string? FailureReason
);
