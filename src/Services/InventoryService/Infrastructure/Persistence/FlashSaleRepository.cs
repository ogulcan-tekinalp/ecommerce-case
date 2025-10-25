using Microsoft.EntityFrameworkCore;
using InventoryService.Application.Abstractions;
using InventoryService.Infrastructure.Persistence;
using InventoryService.Domain.Entities;

namespace InventoryService.Infrastructure.Persistence;

public class FlashSaleRepository : IFlashSaleRepository
{
    private readonly InventoryDbContext _context;

    public FlashSaleRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<FlashSaleProduct?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        return await _context.FlashSaleProducts
            .FirstOrDefaultAsync(fs => fs.ProductId == productId, ct);
    }

    public async Task<List<FlashSaleProduct>> GetActiveFlashSalesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.FlashSaleProducts
            .Where(fs => fs.IsActive && fs.StartTimeUtc <= now && fs.EndTimeUtc >= now)
            .ToListAsync(ct);
    }

    public async Task<FlashSaleProduct> CreateAsync(FlashSaleProduct flashSale, CancellationToken ct = default)
    {
        _context.FlashSaleProducts.Add(flashSale);
        await _context.SaveChangesAsync(ct);
        return flashSale;
    }

    public async Task<FlashSaleProduct> UpdateAsync(FlashSaleProduct flashSale, CancellationToken ct = default)
    {
        _context.FlashSaleProducts.Update(flashSale);
        await _context.SaveChangesAsync(ct);
        return flashSale;
    }

    public async Task<bool> DeleteAsync(Guid flashSaleId, CancellationToken ct = default)
    {
        var flashSale = await _context.FlashSaleProducts
            .FirstOrDefaultAsync(fs => fs.Id == flashSaleId, ct);
        
        if (flashSale == null) return false;

        _context.FlashSaleProducts.Remove(flashSale);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
