using InventoryService.Domain.Entities;

namespace InventoryService.Application.Abstractions;

public interface IFlashSaleRepository
{
    Task<FlashSaleProduct?> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<List<FlashSaleProduct>> GetActiveFlashSalesAsync(CancellationToken ct = default);
    Task<FlashSaleProduct> CreateAsync(FlashSaleProduct flashSale, CancellationToken ct = default);
    Task<FlashSaleProduct> UpdateAsync(FlashSaleProduct flashSale, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid flashSaleId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
