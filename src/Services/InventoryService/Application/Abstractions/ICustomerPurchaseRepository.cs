using InventoryService.Domain.Entities;

namespace InventoryService.Application.Abstractions;

public interface ICustomerPurchaseRepository
{
    Task<List<CustomerPurchase>> GetByCustomerAndProductAsync(Guid customerId, Guid productId, CancellationToken ct = default);
    Task<List<CustomerPurchase>> GetByCustomerAndFlashSaleAsync(Guid customerId, Guid flashSaleProductId, CancellationToken ct = default);
    Task<int> GetTotalQuantityByCustomerAndProductAsync(Guid customerId, Guid productId, CancellationToken ct = default);
    Task<int> GetTotalQuantityByCustomerAndFlashSaleAsync(Guid customerId, Guid flashSaleProductId, CancellationToken ct = default);
    Task<CustomerPurchase> CreateAsync(CustomerPurchase purchase, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
