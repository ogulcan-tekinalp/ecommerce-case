namespace InventoryService.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using InventoryService.Application.Abstractions;
using InventoryService.Application.Inventory.BulkUpdateStock;
using InventoryService.Domain.Entities;
using Xunit;

public class BulkUpdateStockCommandHandlerTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly Mock<ILogger<BulkUpdateStockCommandHandler>> _mockLogger;
    private readonly BulkUpdateStockCommandHandler _handler;

    public BulkUpdateStockCommandHandlerTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _mockLogger = new Mock<ILogger<BulkUpdateStockCommandHandler>>();
        _handler = new BulkUpdateStockCommandHandler(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidBulkUpdate_ShouldReturnSuccess()
    {
        // Arrange
        var product1 = new Product { Id = Guid.NewGuid(), Name = "Product 1", AvailableQuantity = 100 };
        var product2 = new Product { Id = Guid.NewGuid(), Name = "Product 2", AvailableQuantity = 50 };

        var command = new BulkUpdateStockCommand(new List<BulkUpdateStockItemDto>
        {
            new(product1.Id, 10, "Stock addition", true),
            new(product2.Id, 5, "Stock addition", true)
        });

        _mockRepository.Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product1, product2 });
        _mockRepository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.UpdatedCount.Should().Be(2);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new BulkUpdateStockCommand(new List<BulkUpdateStockItemDto>
        {
            new(Guid.NewGuid(), 10, "Stock addition", true)
        });

        _mockRepository.Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.UpdatedCount.Should().Be(0);
        result.Errors.Should().ContainMatch("Products not found*");
    }

    [Fact]
    public async Task Handle_InsufficientStockForSubtraction_ShouldReturnFailure()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Name = "Product 1", AvailableQuantity = 5 };

        var command = new BulkUpdateStockCommand(new List<BulkUpdateStockItemDto>
        {
            new(product.Id, 10, "Stock subtraction", false) // Trying to subtract 10 from 5
        });

        _mockRepository.Setup(x => x.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.UpdatedCount.Should().Be(0);
        result.Errors.Should().ContainMatch("Insufficient stock for product Product 1*");
    }
}
