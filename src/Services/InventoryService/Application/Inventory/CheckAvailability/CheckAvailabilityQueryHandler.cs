namespace InventoryService.Application.Inventory.CheckAvailability;

using MediatR;
using InventoryService.Application.Abstractions;

public sealed class CheckAvailabilityQueryHandler : IRequestHandler<CheckAvailabilityQuery, CheckAvailabilityResult>
{
    private readonly IProductRepository _productRepo;

    public CheckAvailabilityQueryHandler(IProductRepository productRepo)
        => _productRepo = productRepo;

    public async Task<CheckAvailabilityResult> Handle(CheckAvailabilityQuery req, CancellationToken ct)
    {
        var productIds = req.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepo.GetByIdsAsync(productIds, ct);

        var results = req.Items.Select(item =>
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            
            if (product is null)
            {
                return new ProductAvailabilityDto(
                    item.ProductId, "Unknown", item.Quantity, 0, false);
            }

            var isAvailable = product.CanReserve(item.Quantity);
            
            return new ProductAvailabilityDto(
                product.Id,
                product.Name,
                item.Quantity,
                product.AvailableQuantity,
                isAvailable
            );
        }).ToList();

        var allAvailable = results.All(r => r.IsAvailable);

        return new CheckAvailabilityResult(allAvailable, results);
    }
}