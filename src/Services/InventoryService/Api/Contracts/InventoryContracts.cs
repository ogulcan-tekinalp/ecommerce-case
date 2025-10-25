namespace InventoryService.Api.Contracts;

// Existing contracts
public record CheckAvailabilityRequest(List<CheckAvailabilityItemRequest> Items);
public record CheckAvailabilityItemRequest(Guid ProductId, int Quantity);

public record ReserveStockRequest(
    Guid OrderId, 
    List<ReserveStockItemRequest> Items,
    Guid? CustomerId = null
);
public record ReserveStockItemRequest(Guid ProductId, int Quantity);

public record ReleaseStockRequest(Guid ReservationId, string? Reason = null);

// New contracts for enhanced features
public record BulkUpdateStockRequest(List<BulkUpdateStockItemRequest> Items);
public record BulkUpdateStockItemRequest(
    Guid ProductId, 
    int QuantityChange, 
    string Reason, 
    bool IsAddition = true
);

public record ValidateFlashSaleRequest(
    Guid CustomerId,
    Guid ProductId,
    int RequestedQuantity
);
