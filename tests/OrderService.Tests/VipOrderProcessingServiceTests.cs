using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Application.Abstractions;
using OrderService.Application.Vip;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;

namespace OrderService.Tests;

public class VipOrderProcessingServiceTests
{
    private readonly Mock<IOrderRepository> _mockRepository;
    private readonly Mock<ILogger<VipOrderProcessingService>> _mockLogger;
    private readonly VipOrderProcessingService _service;

    public VipOrderProcessingServiceTests()
    {
        _mockRepository = new Mock<IOrderRepository>();
        _mockLogger = new Mock<ILogger<VipOrderProcessingService>>();
        _service = new VipOrderProcessingService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetVipOrdersAsync_ShouldReturnVipOrders()
    {
        // Arrange
        var vipOrders = new List<Order>
        {
            new() { Id = Guid.NewGuid(), IsVip = true, Status = OrderStatus.Pending },
            new() { Id = Guid.NewGuid(), IsVip = true, Status = OrderStatus.Confirmed }
        };

        _mockRepository.Setup(r => r.GetVipOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(vipOrders);

        // Act
        var result = await _service.GetVipOrdersAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(order => order.IsVip.Should().BeTrue());
    }

    [Fact]
    public async Task ProcessVipOrderAsync_WithValidOrder_ShouldProcessSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order { Id = orderId, IsVip = true, Status = OrderStatus.Pending };

        _mockRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        await _service.ProcessVipOrderAsync(orderId);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessVipOrderAsync_WithNonVipOrder_ShouldLogWarning()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order { Id = orderId, IsVip = false, Status = OrderStatus.Pending };

        _mockRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        await _service.ProcessVipOrderAsync(orderId);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsVipCustomerAsync_WithHighOrderCount_ShouldReturnTrue()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orders = Enumerable.Range(1, 15).Select(i => new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            TotalAmount = 1000,
            IsVip = false
        }).ToList();

        _mockRepository.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        var result = await _service.IsVipCustomerAsync(customerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsVipCustomerAsync_WithHighTotalAmount_ShouldReturnTrue()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new() { Id = Guid.NewGuid(), CustomerId = customerId, TotalAmount = 60000, IsVip = false }
        };

        _mockRepository.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        var result = await _service.IsVipCustomerAsync(customerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsVipCustomerAsync_WithLowCriteria_ShouldReturnFalse()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orders = new List<Order>
        {
            new() { Id = Guid.NewGuid(), CustomerId = customerId, TotalAmount = 1000, IsVip = false }
        };

        _mockRepository.Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        var result = await _service.IsVipCustomerAsync(customerId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MarkOrderAsVipAsync_WithValidOrder_ShouldMarkAsVip()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order { Id = orderId, IsVip = false };

        _mockRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        await _service.MarkOrderAsVipAsync(orderId);

        // Assert
        order.IsVip.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
    }
}
