namespace InventoryService.Application.BackgroundJobs;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using InventoryService.Application.Abstractions;


public sealed class StockReservationCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockReservationCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);

    public StockReservationCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<StockReservationCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🧹 Stock Reservation Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredReservationsAsync(stoppingToken);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Stock Reservation Cleanup Service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("🛑 Stock Reservation Cleanup Service stopped");
    }

    private async Task CleanupExpiredReservationsAsync(CancellationToken ct)
    {
        // First, get expired reservation IDs without loading Product navigation
        using var scope = _scopeFactory.CreateScope();
        var reservationRepo = scope.ServiceProvider.GetRequiredService<IStockReservationRepository>();

        var expiredReservations = await reservationRepo.GetExpiredReservationsAsync(ct);

        if (expiredReservations.Count == 0)
        {
            _logger.LogDebug("No expired reservations found");
            return;
        }

        _logger.LogInformation("🧹 Found {Count} expired reservations to cleanup", expiredReservations.Count);

        // Process each reservation in a separate scope/transaction to avoid concurrency conflicts
        foreach (var reservation in expiredReservations)
        {
            try
            {
                // Create fresh scope for each reservation to avoid tracking conflicts
                using var itemScope = _scopeFactory.CreateScope();
                var itemReservationRepo = itemScope.ServiceProvider.GetRequiredService<IStockReservationRepository>();
                var itemProductRepo = itemScope.ServiceProvider.GetRequiredService<IProductRepository>();

                // Reload reservation with product in new context
                var freshReservation = await itemReservationRepo.GetByIdAsync(reservation.Id, ct);
                
                if (freshReservation == null || freshReservation.IsReleased)
                {
                    _logger.LogDebug("Reservation {ReservationId} already processed", reservation.Id);
                    continue;
                }

                // Release stock back to product
                freshReservation.Product.Release(freshReservation.Quantity);
                freshReservation.Release("Expired - automatic cleanup after 10 minutes");

                await itemProductRepo.SaveChangesAsync(ct);

                _logger.LogInformation("✅ Released expired reservation {ReservationId} for Order {OrderId}",
                    freshReservation.Id, freshReservation.OrderId);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "⚠️ Concurrency conflict for reservation {ReservationId} - already processed by another instance",
                    reservation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to cleanup reservation {ReservationId}", reservation.Id);
            }
        }
    }
}