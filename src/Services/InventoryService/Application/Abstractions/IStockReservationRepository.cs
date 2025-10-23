namespace InventoryService.Application.Abstractions;

using InventoryService.Domain.Entities;

public interface IStockReservationRepository
{
    Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StockReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<List<StockReservation>> GetExpiredReservationsAsync(CancellationToken ct = default);
    Task AddAsync(StockReservation reservation, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}