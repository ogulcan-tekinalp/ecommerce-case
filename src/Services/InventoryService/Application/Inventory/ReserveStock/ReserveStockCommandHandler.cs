namespace InventoryService.Application.Inventory.ReserveStock;

using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using InventoryService.Application.Abstractions;
using InventoryService.Domain.Entities;
using InventoryService.Application.Inventory.ValidateFlashSale;

public sealed class ReserveStockCommandHandler : IRequestHandler<ReserveStockCommand, ReserveStockResult>
{
    private readonly IProductRepository _productRepo;
    private readonly IStockReservationRepository _reservationRepo;
    private readonly ISender _mediator;
    private readonly ILogger<ReserveStockCommandHandler> _logger;

    public ReserveStockCommandHandler(
        IProductRepository productRepo,
        IStockReservationRepository reservationRepo,
        ISender mediator,
        ILogger<ReserveStockCommandHandler> logger)
    {
        _productRepo = productRepo;
        _reservationRepo = reservationRepo;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ReserveStockResult> Handle(ReserveStockCommand req, CancellationToken ct)
    {
        var productIds = req.Items.Select(i => i.ProductId).ToList();
        var products = await _productRepo.GetByIdsAsync(productIds, ct);

        // Check if all products exist
        if (products.Count != productIds.Count)
        {
            var missingIds = productIds.Except(products.Select(p => p.Id)).ToList();
            _logger.LogWarning("Products not found: {ProductIds}", string.Join(", ", missingIds));
            return new ReserveStockResult(false, null, "Some products not found");
        }

        // Check stock availability and flash sale limits for each product
        foreach (var item in req.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            
            if (!product.CanReserve(item.Quantity))
            {
                _logger.LogWarning("Insufficient stock for Product {ProductId}. Available: {Available}, Requested: {Requested}",
                    product.Id, product.AvailableQuantity, item.Quantity);
                return new ReserveStockResult(false, null, 
                    $"Insufficient stock for {product.Name}. Available: {product.AvailableQuantity}");
            }

            // Business Rule: Cannot reserve more than 50% of available stock
            if (item.Quantity > product.TotalQuantity * 0.5m)
            {
                _logger.LogWarning("Cannot reserve more than 50% of total stock for Product {ProductId}", product.Id);
                return new ReserveStockResult(false, null, 
                    $"Cannot reserve more than 50% of stock for {product.Name}");
            }

            // Check flash sale limits (if customer ID is provided)
            if (req.CustomerId.HasValue)
            {
                var flashSaleValidation = await _mediator.Send(new ValidateFlashSaleCommand(
                    req.CustomerId.Value, item.ProductId, item.Quantity), ct);

                if (!flashSaleValidation.IsValid)
                {
                    _logger.LogWarning("Flash sale validation failed for Product {ProductId}: {Reason}",
                        item.ProductId, flashSaleValidation.FailureReason);
                    return new ReserveStockResult(false, null, flashSaleValidation.FailureReason);
                }
            }
        }

        try
        {
            // Reserve stock for all products
            foreach (var item in req.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                product.Reserve(item.Quantity);
            }

            // Create reservation record (10 minute expiration)
            var reservationId = Guid.NewGuid();
            var reservation = new StockReservation
            {
                Id = reservationId,
                OrderId = req.OrderId,
                ProductId = req.Items.First().ProductId, // For simplicity, store first product
                Quantity = req.Items.Sum(i => i.Quantity),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
            };

            await _reservationRepo.AddAsync(reservation, ct);
            await _productRepo.SaveChangesAsync(ct);

            _logger.LogInformation("Stock reserved for Order {OrderId}, Reservation {ReservationId}, Expires at {ExpiresAt}",
                req.OrderId, reservationId, reservation.ExpiresAtUtc);

            return new ReserveStockResult(true, reservationId, null);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Optimistic locking failure - race condition detected!
            _logger.LogWarning(ex, "Concurrency conflict while reserving stock for Order {OrderId}", req.OrderId);
            return new ReserveStockResult(false, null, "Stock was modified by another transaction. Please retry.");
        }
    }
}