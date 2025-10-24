using MediatR;
using InventoryService.Application.Abstractions;

namespace InventoryService.Application.Inventory.GetProductStock;

public class GetProductStockQueryHandler : IRequestHandler<GetProductStockQuery, GetProductStockResult?>
{
    private readonly IProductRepository _repository;

    public GetProductStockQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetProductStockResult?> Handle(GetProductStockQuery request, CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(request.ProductId, ct);
        
        if (product == null)
            return null;

        return new GetProductStockResult(
            product.Id,
            product.Name,
            product.AvailableQuantity,
            product.ReservedQuantity,
            product.Price
        );
    }
}