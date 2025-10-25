using MediatR;
using Microsoft.Extensions.Logging;
using InventoryService.Application.Abstractions;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Inventory.BulkUpdateStock;

public class BulkUpdateStockCommandHandler : IRequestHandler<BulkUpdateStockCommand, BulkUpdateStockResult>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<BulkUpdateStockCommandHandler> _logger;

    public BulkUpdateStockCommandHandler(
        IProductRepository productRepository,
        ILogger<BulkUpdateStockCommandHandler> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<BulkUpdateStockResult> Handle(BulkUpdateStockCommand request, CancellationToken ct)
    {
        _logger.LogInformation("🔄 Processing bulk stock update for {ItemCount} items", request.Items.Count);

        var errors = new List<string>();
        var updatedCount = 0;
        var productIds = request.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepository.GetByIdsAsync(productIds, ct);

        // Validate all products exist
        var missingProductIds = productIds.Except(products.Select(p => p.Id)).ToList();
        if (missingProductIds.Any())
        {
            errors.Add($"Products not found: {string.Join(", ", missingProductIds)}");
            return new BulkUpdateStockResult(false, 0, errors);
        }

        // Group products by ID for easier lookup
        var productDict = products.ToDictionary(p => p.Id);

        // Validate all operations before applying any changes
        foreach (var item in request.Items)
        {
            var product = productDict[item.ProductId];
            
            // Validate quantity change
            if (item.QuantityChange <= 0)
            {
                errors.Add($"Invalid quantity change for product {product.Name}: {item.QuantityChange}");
                continue;
            }

            // For subtractions, check if we have enough available stock
            if (!item.IsAddition)
            {
                var availableAfterChange = product.AvailableQuantity - item.QuantityChange;
                if (availableAfterChange < 0)
                {
                    errors.Add($"Insufficient stock for product {product.Name}. Available: {product.AvailableQuantity}, Requested: {item.QuantityChange}");
                    continue;
                }
            }
        }

        // If there are validation errors, return early
        if (errors.Any())
        {
            return new BulkUpdateStockResult(false, 0, errors);
        }

        // Apply all changes
        try
        {
            foreach (var item in request.Items)
            {
                var product = productDict[item.ProductId];
                
                if (item.IsAddition)
                {
                    product.AvailableQuantity += item.QuantityChange;
                    _logger.LogInformation("➕ Added {Quantity} to product {ProductName} (ID: {ProductId})",
                        item.QuantityChange, product.Name, product.Id);
                }
                else
                {
                    product.AvailableQuantity -= item.QuantityChange;
                    _logger.LogInformation("➖ Subtracted {Quantity} from product {ProductName} (ID: {ProductId})",
                        item.QuantityChange, product.Name, product.Id);
                }

                product.UpdatedAtUtc = DateTime.UtcNow;
                updatedCount++;
            }

            // Save all changes in a single transaction
            await _productRepository.SaveChangesAsync(ct);

            _logger.LogInformation("✅ Bulk stock update completed successfully. Updated {Count} products", updatedCount);
            return new BulkUpdateStockResult(true, updatedCount, new List<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to process bulk stock update");
            return new BulkUpdateStockResult(false, updatedCount, new List<string> { $"Internal error: {ex.Message}" });
        }
    }
}
