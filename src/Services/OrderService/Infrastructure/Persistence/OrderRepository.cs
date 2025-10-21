using Microsoft.EntityFrameworkCore;
using OrderService.Application.Abstractions;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _db;
    public OrderRepository(OrderDbContext db) => _db = db;

    public Task AddAsync(Order order, CancellationToken ct = default)
        => _db.Orders.AddAsync(order, ct).AsTask();

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
