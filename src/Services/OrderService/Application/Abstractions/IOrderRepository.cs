using OrderService.Domain.Entities;

namespace OrderService.Application.Abstractions;


public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default); // ⚡ YENİ
    Task SaveChangesAsync(CancellationToken ct = default);
}
