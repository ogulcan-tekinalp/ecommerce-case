using Microsoft.Extensions.Logging;
using InventoryService.Application.Abstractions;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Inventory.LowStockAlert;

public interface ILowStockAlertService
{
    Task CheckAndSendLowStockAlertsAsync(CancellationToken ct = default);
    Task<List<Product>> GetLowStockProductsAsync(CancellationToken ct = default);
}

public class LowStockAlertService : ILowStockAlertService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<LowStockAlertService> _logger;
    private readonly HashSet<Guid> _alertedProducts = new();

    public LowStockAlertService(
        IProductRepository productRepository,
        ILogger<LowStockAlertService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task CheckAndSendLowStockAlertsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("🔍 Checking for low stock products...");

        var lowStockProducts = await GetLowStockProductsAsync(ct);
        
        if (!lowStockProducts.Any())
        {
            _logger.LogInformation("✅ No low stock products found");
            return;
        }

        foreach (var product in lowStockProducts)
        {
            // Only send alert once per product to avoid spam
            if (_alertedProducts.Contains(product.Id))
            {
                _logger.LogDebug("Alert already sent for product {ProductId} ({ProductName})", 
                    product.Id, product.Name);
                continue;
            }

            await SendLowStockAlertAsync(product);
            _alertedProducts.Add(product.Id);
        }

        _logger.LogInformation("📧 Sent {Count} low stock alerts", lowStockProducts.Count);
    }

    public async Task<List<Product>> GetLowStockProductsAsync(CancellationToken ct = default)
    {
        var allProducts = await _productRepository.GetAllAsync(ct);
        return allProducts.Where(p => p.IsLowStock()).ToList();
    }

    private async Task SendLowStockAlertAsync(Product product)
    {
        _logger.LogWarning("🚨 LOW STOCK ALERT: Product '{ProductName}' (ID: {ProductId}) has only {AvailableQuantity} items available!",
            product.Name, product.Id, product.AvailableQuantity);

        // In a real implementation, you would:
        // 1. Send email to inventory managers
        // 2. Send Slack/Teams notification
        // 3. Create a ticket in your ticketing system
        // 4. Send SMS to warehouse staff
        
        // For now, we'll just log the alert
        await Task.Delay(100); // Simulate async operation
        
        _logger.LogInformation("📧 Low stock alert sent for product {ProductName} (ID: {ProductId})",
            product.Name, product.Id);
    }

    public void ResetAlertForProduct(Guid productId)
    {
        _alertedProducts.Remove(productId);
        _logger.LogInformation("🔄 Reset low stock alert for product {ProductId}", productId);
    }
}
