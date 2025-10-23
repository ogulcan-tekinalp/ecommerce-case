namespace InventoryService.Api.Controllers;

public sealed record ReleaseStockRequest(Guid ReservationId, string? Reason);