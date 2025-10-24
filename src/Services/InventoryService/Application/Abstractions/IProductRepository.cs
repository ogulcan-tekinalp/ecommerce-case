namespace InventoryService.Application.Abstractions;
using InventoryService.Domain.Entities;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task<List<StockReservation>> GetExpiredReservationsAsync(CancellationToken ct = default);
    Task ReleaseReservationAsync(Guid reservationId, string reason, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}