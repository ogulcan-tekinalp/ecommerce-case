namespace InventoryService.Api.Controllers;

public sealed record ReserveStockRequest(Guid OrderId, List<ReserveStockItemRequest> Items, Guid? CustomerId = null);
public record ReserveStockItemRequest(Guid ProductId, int Quantity);