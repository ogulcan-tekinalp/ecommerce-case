namespace OrderService.Application.Services;

public interface IInventoryServiceClient
{
    Task<CheckAvailabilityResponse> CheckAvailabilityAsync(
        List<CheckAvailabilityItemRequest> items,
        CancellationToken cancellationToken = default);

    Task<ReserveStockResponse> ReserveStockAsync(
        Guid orderId,
        List<ReserveStockItemRequest> items,
        CancellationToken cancellationToken = default);

    Task<bool> ReleaseStockAsync(
        Guid reservationId,
        string reason,
        CancellationToken cancellationToken = default);
}

// DTOs
public record CheckAvailabilityItemRequest(Guid ProductId, int Quantity);
public record CheckAvailabilityResponse(bool IsAvailable, List<string> UnavailableProducts);

public record ReserveStockItemRequest(Guid ProductId, int Quantity);
public record ReserveStockResponse(bool Success, Guid? ReservationId, string? FailureReason);
