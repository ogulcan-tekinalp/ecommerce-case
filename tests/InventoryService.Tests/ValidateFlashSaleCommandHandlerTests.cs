namespace InventoryService.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using InventoryService.Application.Abstractions;
using InventoryService.Application.Inventory.ValidateFlashSale;
using InventoryService.Domain.Entities;
using Xunit;

public class ValidateFlashSaleCommandHandlerTests
{
    private readonly Mock<IFlashSaleRepository> _mockFlashSaleRepository;
    private readonly Mock<ICustomerPurchaseRepository> _mockCustomerPurchaseRepository;
    private readonly Mock<ILogger<ValidateFlashSaleCommandHandler>> _mockLogger;
    private readonly ValidateFlashSaleCommandHandler _handler;

    public ValidateFlashSaleCommandHandlerTests()
    {
        _mockFlashSaleRepository = new Mock<IFlashSaleRepository>();
        _mockCustomerPurchaseRepository = new Mock<ICustomerPurchaseRepository>();
        _mockLogger = new Mock<ILogger<ValidateFlashSaleCommandHandler>>();
        _handler = new ValidateFlashSaleCommandHandler(
            _mockFlashSaleRepository.Object, 
            _mockCustomerPurchaseRepository.Object, 
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_NotFlashSaleProduct_ShouldReturnValid()
    {
        // Arrange
        var command = new ValidateFlashSaleCommand(Guid.NewGuid(), Guid.NewGuid(), 5);
        _mockFlashSaleRepository.Setup(x => x.GetByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlashSaleProduct?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeTrue();
        result.IsFlashSale.Should().BeFalse();
        result.MaxAllowedQuantity.Should().Be(int.MaxValue);
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_FlashSaleProductWithinLimit_ShouldReturnValid()
    {
        // Arrange
        var flashSale = new FlashSaleProduct
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            MaxQuantityPerCustomer = 2,
            StartTimeUtc = DateTime.UtcNow.AddHours(-1),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            IsActive = true
        };

        var command = new ValidateFlashSaleCommand(Guid.NewGuid(), flashSale.ProductId, 1);
        
        _mockFlashSaleRepository.Setup(x => x.GetByProductIdAsync(flashSale.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flashSale);
        _mockCustomerPurchaseRepository.Setup(x => x.GetTotalQuantityByCustomerAndFlashSaleAsync(
            It.IsAny<Guid>(), flashSale.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeTrue();
        result.IsFlashSale.Should().BeTrue();
        result.MaxAllowedQuantity.Should().Be(2);
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_FlashSaleProductExceedsLimit_ShouldReturnInvalid()
    {
        // Arrange
        var flashSale = new FlashSaleProduct
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            MaxQuantityPerCustomer = 2,
            StartTimeUtc = DateTime.UtcNow.AddHours(-1),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            IsActive = true
        };

        var command = new ValidateFlashSaleCommand(Guid.NewGuid(), flashSale.ProductId, 3);
        
        _mockFlashSaleRepository.Setup(x => x.GetByProductIdAsync(flashSale.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flashSale);
        _mockCustomerPurchaseRepository.Setup(x => x.GetTotalQuantityByCustomerAndFlashSaleAsync(
            It.IsAny<Guid>(), flashSale.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeFalse();
        result.IsFlashSale.Should().BeTrue();
        result.MaxAllowedQuantity.Should().Be(2);
        result.FailureReason.Should().Contain("You can only purchase 2 more items");
    }

    [Fact]
    public async Task Handle_CustomerReachedLimit_ShouldReturnInvalid()
    {
        // Arrange
        var flashSale = new FlashSaleProduct
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            MaxQuantityPerCustomer = 2,
            StartTimeUtc = DateTime.UtcNow.AddHours(-1),
            EndTimeUtc = DateTime.UtcNow.AddHours(1),
            IsActive = true
        };

        var command = new ValidateFlashSaleCommand(Guid.NewGuid(), flashSale.ProductId, 1);
        
        _mockFlashSaleRepository.Setup(x => x.GetByProductIdAsync(flashSale.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flashSale);
        _mockCustomerPurchaseRepository.Setup(x => x.GetTotalQuantityByCustomerAndFlashSaleAsync(
            It.IsAny<Guid>(), flashSale.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2); // Customer already purchased 2 items

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeFalse();
        result.IsFlashSale.Should().BeTrue();
        result.MaxAllowedQuantity.Should().Be(0);
        result.FailureReason.Should().Contain("Flash sale limit reached");
    }
}
