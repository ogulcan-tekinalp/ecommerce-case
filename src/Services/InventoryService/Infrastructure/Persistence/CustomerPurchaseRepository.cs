using Microsoft.EntityFrameworkCore;
using InventoryService.Application.Abstractions;
using InventoryService.Infrastructure.Persistence;
using InventoryService.Domain.Entities;

namespace InventoryService.Infrastructure.Persistence;

public class CustomerPurchaseRepository : ICustomerPurchaseRepository
{
    private readonly InventoryDbContext _context;

    public CustomerPurchaseRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<List<CustomerPurchase>> GetByCustomerAndProductAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        return await _context.CustomerPurchases
            .Where(cp => cp.CustomerId == customerId && cp.ProductId == productId)
            .OrderByDescending(cp => cp.PurchaseDateUtc)
            .ToListAsync(ct);
    }

    public async Task<List<CustomerPurchase>> GetByCustomerAndFlashSaleAsync(Guid customerId, Guid flashSaleProductId, CancellationToken ct = default)
    {
        return await _context.CustomerPurchases
            .Where(cp => cp.CustomerId == customerId && cp.FlashSaleProductId == flashSaleProductId)
            .OrderByDescending(cp => cp.PurchaseDateUtc)
            .ToListAsync(ct);
    }

    public async Task<int> GetTotalQuantityByCustomerAndProductAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        return await _context.CustomerPurchases
            .Where(cp => cp.CustomerId == customerId && cp.ProductId == productId)
            .SumAsync(cp => cp.Quantity, ct);
    }

    public async Task<int> GetTotalQuantityByCustomerAndFlashSaleAsync(Guid customerId, Guid flashSaleProductId, CancellationToken ct = default)
    {
        return await _context.CustomerPurchases
            .Where(cp => cp.CustomerId == customerId && cp.FlashSaleProductId == flashSaleProductId)
            .SumAsync(cp => cp.Quantity, ct);
    }

    public async Task<CustomerPurchase> CreateAsync(CustomerPurchase purchase, CancellationToken ct = default)
    {
        _context.CustomerPurchases.Add(purchase);
        await _context.SaveChangesAsync(ct);
        return purchase;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
