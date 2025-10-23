namespace InventoryService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using InventoryService.Application.Abstractions;
using InventoryService.Domain.Entities;

public sealed class StockReservationRepository : IStockReservationRepository
{
    private readonly InventoryDbContext _db;
    public StockReservationRepository(InventoryDbContext db) => _db = db;

    public Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.StockReservations
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<StockReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        => _db.StockReservations
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

    public Task<List<StockReservation>> GetExpiredReservationsAsync(CancellationToken ct = default)
        => _db.StockReservations
            .Include(x => x.Product)
            .Where(x => !x.IsReleased && x.ExpiresAtUtc < DateTime.UtcNow)
            .ToListAsync(ct);

    public Task AddAsync(StockReservation reservation, CancellationToken ct = default)
        => _db.StockReservations.AddAsync(reservation, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
