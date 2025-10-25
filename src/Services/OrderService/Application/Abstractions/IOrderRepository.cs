using OrderService.Domain.Entities;

namespace OrderService.Application.Abstractions;


public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);

    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);
    
    // VIP Order methods
    Task<List<Order>> GetVipOrdersAsync(CancellationToken ct = default);
    Task<List<Order>> GetPendingVipOrdersAsync(CancellationToken ct = default);
}
