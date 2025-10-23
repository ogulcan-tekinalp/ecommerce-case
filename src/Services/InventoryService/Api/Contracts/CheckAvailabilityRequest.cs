namespace InventoryService.Api.Controllers;

public sealed record CheckAvailabilityRequest(List<CheckAvailabilityItemRequest> Items);
public record CheckAvailabilityItemRequest(Guid ProductId, int Quantity);