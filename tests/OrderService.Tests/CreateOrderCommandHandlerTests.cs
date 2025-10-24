namespace OrderService.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Application.Abstractions;
using OrderService.Application.Orders.CreateOrder;
using OrderService.Application.Sagas;
using OrderService.Domain.Entities;
using Xunit;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _mockRepo;
    private readonly Mock<OrderSaga> _mockSaga;
    private readonly Mock<ILogger<CreateOrderCommandHandler>> _mockLogger;
    private readonly CreateOrderCommandHandler _handler;
    private readonly Faker _faker;

    public CreateOrderCommandHandlerTests()
    {
        _mockRepo = new Mock<IOrderRepository>();
        _mockSaga = new Mock<OrderSaga>(MockBehavior.Loose, null, null, null);
        _mockLogger = new Mock<ILogger<CreateOrderCommandHandler>>();
        _handler = new CreateOrderCommandHandler(_mockRepo.Object, _mockSaga.Object, _mockLogger.Object);
        _faker = new Faker();
    }

    [Fact]
    public async Task Handle_ValidOrder_ReturnsOrderId()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new CreateOrderCommand(
            CustomerId: customerId,
            IsVip: false,
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 2, 500m)
            }
        );

        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty("because a valid order should return an OrderId");
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VipCustomer_SetsIsVipTrue()
    {
        // Arrange
        Order? capturedOrder = null;
        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order);

        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: true, // VIP customer
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "VIP Product", 1, 1000m)
            }
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedOrder.Should().NotBeNull();
        capturedOrder!.IsVip.Should().BeTrue("because VIP flag should be preserved");
    }

    [Fact]
    public async Task Handle_MultipleItems_CalculatesCorrectTotal()
    {
        // Arrange
        Order? capturedOrder = null;
        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order);

        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: false,
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Product 1", 2, 100m), // 200
                new(Guid.NewGuid(), "Product 2", 3, 50m),  // 150
                new(Guid.NewGuid(), "Product 3", 1, 250m)  // 250
            }
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedOrder.Should().NotBeNull();
        capturedOrder!.TotalAmount.Should().Be(600m, "because 2*100 + 3*50 + 1*250 = 600");
        capturedOrder.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithIdempotencyKey_PreventsDuplicateOrders()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var existingOrderId = Guid.NewGuid();
        
        var existingOrder = new Order
        {
            Id = existingOrderId,
            CustomerId = Guid.NewGuid(),
            TotalAmount = 500m
        };
        existingOrder.SetIdempotencyKey(idempotencyKey);

        _mockRepo
            .Setup(r => r.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: false,
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, 500m)
            },
            IdempotencyKey: idempotencyKey
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(existingOrderId, "because duplicate order should return existing order ID");
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never,
            "because no new order should be created for duplicate request");
    }

    [Theory]
    [InlineData(1, 100, 100)]
    [InlineData(5, 50, 250)]
    [InlineData(10, 25.50, 255)]
    public async Task Handle_DifferentQuantitiesAndPrices_CalculatesCorrectly(int quantity, decimal price, decimal expectedTotal)
    {
        // Arrange
        Order? capturedOrder = null;
        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order);

        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: false,
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", quantity, price)
            }
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedOrder!.TotalAmount.Should().Be(expectedTotal);
    }

    [Fact]
    public async Task Handle_OrderItems_ContainCorrectData()
    {
        // Arrange
        Order? capturedOrder = null;
        _mockRepo
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order);

        var productId = Guid.NewGuid();
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: false,
            Items: new List<CreateOrderItemDto>
            {
                new(productId, "Laptop Dell XPS 15", 2, 15000m)
            }
        );

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedOrder!.Items.Should().HaveCount(1);
        var item = capturedOrder.Items.First();
        item.ProductId.Should().Be(productId);
        item.ProductName.Should().Be("Laptop Dell XPS 15");
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(15000m);
        item.TotalPrice.Should().Be(30000m);
    }
}