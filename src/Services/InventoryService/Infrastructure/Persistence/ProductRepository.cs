namespace InventoryService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using InventoryService.Application.Abstractions;
using InventoryService.Domain.Entities;

public sealed class ProductRepository : IProductRepository
{
    private readonly InventoryDbContext _db;
    public ProductRepository(InventoryDbContext db) => _db = db;

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default)
        => _db.Products.Where(x => ids.Contains(x.Id)).ToListAsync(ct);

    public Task AddAsync(Product product, CancellationToken ct = default)
        => _db.Products.AddAsync(product, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}