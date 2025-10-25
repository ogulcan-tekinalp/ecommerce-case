using MediatR;
using Microsoft.Extensions.Logging;
using InventoryService.Application.Abstractions;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Inventory.ValidateFlashSale;

public class ValidateFlashSaleCommandHandler : IRequestHandler<ValidateFlashSaleCommand, ValidateFlashSaleResult>
{
    private readonly IFlashSaleRepository _flashSaleRepository;
    private readonly ICustomerPurchaseRepository _customerPurchaseRepository;
    private readonly ILogger<ValidateFlashSaleCommandHandler> _logger;

    public ValidateFlashSaleCommandHandler(
        IFlashSaleRepository flashSaleRepository,
        ICustomerPurchaseRepository customerPurchaseRepository,
        ILogger<ValidateFlashSaleCommandHandler> logger)
    {
        _flashSaleRepository = flashSaleRepository;
        _customerPurchaseRepository = customerPurchaseRepository;
        _logger = logger;
    }

    public async Task<ValidateFlashSaleResult> Handle(ValidateFlashSaleCommand request, CancellationToken ct)
    {
        _logger.LogInformation("🔍 Validating flash sale for Customer {CustomerId}, Product {ProductId}, Quantity {Quantity}",
            request.CustomerId, request.ProductId, request.RequestedQuantity);

        // Check if product is part of an active flash sale
        var flashSale = await _flashSaleRepository.GetByProductIdAsync(request.ProductId, ct);
        
        if (flashSale == null || !flashSale.IsCurrentlyActive())
        {
            _logger.LogInformation("Product {ProductId} is not part of an active flash sale", request.ProductId);
            return new ValidateFlashSaleResult(
                IsValid: true,
                IsFlashSale: false,
                MaxAllowedQuantity: int.MaxValue, // No limit for regular products
                FailureReason: null
            );
        }

        _logger.LogInformation("🎯 Product {ProductId} is part of active flash sale {FlashSaleId}", 
            request.ProductId, flashSale.Id);

        // Check customer's previous purchases for this flash sale
        var totalPurchased = await _customerPurchaseRepository.GetTotalQuantityByCustomerAndFlashSaleAsync(
            request.CustomerId, flashSale.Id, ct);

        var remainingQuantity = flashSale.MaxQuantityPerCustomer - totalPurchased;

        if (remainingQuantity <= 0)
        {
            _logger.LogWarning("Customer {CustomerId} has reached flash sale limit for product {ProductId}. Total purchased: {TotalPurchased}, Limit: {Limit}",
                request.CustomerId, request.ProductId, totalPurchased, flashSale.MaxQuantityPerCustomer);

            return new ValidateFlashSaleResult(
                IsValid: false,
                IsFlashSale: true,
                MaxAllowedQuantity: 0,
                FailureReason: $"Flash sale limit reached. You can only purchase {flashSale.MaxQuantityPerCustomer} items per customer."
            );
        }

        if (request.RequestedQuantity > remainingQuantity)
        {
            _logger.LogWarning("Customer {CustomerId} requested {Requested} items but only {Remaining} allowed for flash sale product {ProductId}",
                request.CustomerId, request.RequestedQuantity, remainingQuantity, request.ProductId);

            return new ValidateFlashSaleResult(
                IsValid: false,
                IsFlashSale: true,
                MaxAllowedQuantity: remainingQuantity,
                FailureReason: $"You can only purchase {remainingQuantity} more items for this flash sale product."
            );
        }

        _logger.LogInformation("✅ Flash sale validation passed for Customer {CustomerId}, Product {ProductId}",
            request.CustomerId, request.ProductId);

        return new ValidateFlashSaleResult(
            IsValid: true,
            IsFlashSale: true,
            MaxAllowedQuantity: remainingQuantity,
            FailureReason: null
        );
    }
}
