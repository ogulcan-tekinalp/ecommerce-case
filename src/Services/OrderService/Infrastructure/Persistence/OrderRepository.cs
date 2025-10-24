namespace OrderService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Abstractions;
using OrderService.Domain.Entities;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _db;
    
    public OrderRepository(OrderDbContext db) => _db = db;
    
    public Task AddAsync(Order order, CancellationToken ct = default)
        => _db.Orders.AddAsync(order, ct).AsTask();
    
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    
    public Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(ct);
    
    public Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
        => _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey, ct);
    
    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}