using System.Net.Http.Json;
using System.Text.Json;
using OrderService.Application.Services;
using Microsoft.Extensions.Logging;


namespace OrderService.Infrastructure.Services;

public class InventoryServiceClient : IInventoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryServiceClient> _logger;

    public InventoryServiceClient(HttpClient httpClient, ILogger<InventoryServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CheckAvailabilityResponse> CheckAvailabilityAsync(
        List<CheckAvailabilityItemRequest> items,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                items = items.Select(i => new { productId = i.ProductId, quantity = i.Quantity }).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/inventory/check-availability",
                requestBody,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CheckAvailabilityResponse>(
                cancellationToken: cancellationToken);

            return result ?? new CheckAvailabilityResponse(false, new List<string> { "Unknown error" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock availability");
            return new CheckAvailabilityResponse(false, new List<string> { ex.Message });
        }
    }

    public async Task<ReserveStockResponse> ReserveStockAsync(
        Guid orderId,
        List<ReserveStockItemRequest> items,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                orderId,
                items = items.Select(i => new { productId = i.ProductId, quantity = i.Quantity }).ToList()
            };

            _logger.LogInformation("🔄 Sending reserve stock request for order {OrderId}", orderId);

            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/inventory/reserve",
                requestBody,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("❌ Reserve stock failed: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                
                return new ReserveStockResponse(false, null, $"HTTP {response.StatusCode}: {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<ReserveStockApiResponse>(
                cancellationToken: cancellationToken);

            if (result?.ReservationId != null)
            {
                _logger.LogInformation("✅ Stock reserved successfully. ReservationId: {ReservationId}", 
                    result.ReservationId);
                return new ReserveStockResponse(true, result.ReservationId, null);
            }

            return new ReserveStockResponse(false, null, "Invalid response from inventory service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error reserving stock for order {OrderId}", orderId);
            return new ReserveStockResponse(false, null, ex.Message);
        }
    }

    public async Task<bool> ReleaseStockAsync(
        Guid reservationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                reservationId,
                reason
            };

            _logger.LogInformation("🔄 Releasing stock reservation {ReservationId}", reservationId);

            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/inventory/release",
                requestBody,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("❌ Release stock failed: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                return false;
            }

            _logger.LogInformation("✅ Stock released successfully for reservation {ReservationId}", 
                reservationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error releasing stock reservation {ReservationId}", reservationId);
            return false;
        }
    }

    // Internal DTO for API response
    private record ReserveStockApiResponse(Guid ReservationId);
}
