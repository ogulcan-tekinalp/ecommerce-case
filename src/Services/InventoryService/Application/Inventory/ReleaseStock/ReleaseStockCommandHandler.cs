namespace InventoryService.Application.Inventory.ReleaseStock;

using MediatR;
using Microsoft.Extensions.Logging;
using InventoryService.Application.Abstractions;

public sealed class ReleaseStockCommandHandler : IRequestHandler<ReleaseStockCommand, bool>
{
    private readonly IStockReservationRepository _reservationRepo;
    private readonly IProductRepository _productRepo;
    private readonly ILogger<ReleaseStockCommandHandler> _logger;

    public ReleaseStockCommandHandler(
        IStockReservationRepository reservationRepo,
        IProductRepository productRepo,
        ILogger<ReleaseStockCommandHandler> logger)
    {
        _reservationRepo = reservationRepo;
        _productRepo = productRepo;
        _logger = logger;
    }

    public async Task<bool> Handle(ReleaseStockCommand req, CancellationToken ct)
    {
        var reservation = await _reservationRepo.GetByIdAsync(req.ReservationId, ct);
        
        if (reservation is null)
        {
            _logger.LogWarning("Reservation {ReservationId} not found", req.ReservationId);
            return false;
        }

        if (reservation.IsReleased)
        {
            _logger.LogWarning("Reservation {ReservationId} already released", req.ReservationId);
            return false;
        }

        // Release stock back to product
        reservation.Product.Release(reservation.Quantity);
        reservation.Release(req.Reason);

        await _productRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Stock released for Reservation {ReservationId}, Order {OrderId}, Reason: {Reason}",
            req.ReservationId, reservation.OrderId, req.Reason);

        return true;
    }
}