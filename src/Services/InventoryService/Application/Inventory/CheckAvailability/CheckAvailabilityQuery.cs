namespace InventoryService.Application.Inventory.CheckAvailability;

using MediatR;

public sealed record CheckAvailabilityQuery(
    List<CheckAvailabilityItemDto> Items
) : IRequest<CheckAvailabilityResult>;

public record CheckAvailabilityItemDto(Guid ProductId, int Quantity);

public record CheckAvailabilityResult(
    bool AllAvailable,
    List<ProductAvailabilityDto> Products
);

public record ProductAvailabilityDto(
    Guid ProductId,
    string ProductName,
    int RequestedQuantity,
    int AvailableQuantity,
    bool IsAvailable
);