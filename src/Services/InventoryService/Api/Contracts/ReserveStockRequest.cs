namespace InventoryService.Api.Controllers;

public sealed record ReserveStockRequest(Guid OrderId, List<ReserveStockItemRequest> Items);
public record ReserveStockItemRequest(Guid ProductId, int Quantity);