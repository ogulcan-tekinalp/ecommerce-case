namespace OrderService.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentValidation.TestHelper;
using OrderService.Application.Orders.CreateOrder;
using Xunit;

/// <summary>
/// ✅ CASE REQUIREMENT: Business rules validation
/// - Minimum order: 100 TL
/// - Maximum order: 50,000 TL
/// - Max 20 items per order
/// </summary>
public class CreateOrderValidationTests
{
    private readonly CreateOrderCommandValidator _validator;

    public CreateOrderValidationTests()
    {
        _validator = new CreateOrderCommandValidator();
    }

    [Fact]
    public void Validate_EmptyItems_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: false,
            Items: new List<CreateOrderItemDto>()
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Order must contain at least one item");
    }

    [Fact]
    public void Validate_TotalBelow100TL_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: false,
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Cheap Item", 1, 50m) // Only 50 TL
            }
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Minimum order amount is 100 TL");
    }

    [Fact]
    public void Validate_TotalAbove50000TL_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: false,
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Expensive Item", 10, 6000m) // 60,000 TL
            }
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Maximum order amount is 50,000 TL");
    }

    [Fact]
    public void Validate_MoreThan20Items_ShouldHaveValidationError()
    {
        // Arrange
        var items = Enumerable.Range(1, 21)
            .Select(i => new CreateOrderItemDto(Guid.NewGuid(), $"Product {i}", 1, 100m))
            .ToList();

        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: false,
            Items: items
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("Order cannot contain more than 20 items");
    }

    [Fact]
    public void Validate_ValidOrder_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: false,
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Valid Product", 2, 500m) // 1000 TL
            }
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(100, true)]   // Exactly minimum - valid
    [InlineData(50000, true)] // Exactly maximum - valid
    [InlineData(99, false)]   // Below minimum - invalid
    [InlineData(50001, false)] // Above maximum - invalid
    public void Validate_BoundaryValues_ReturnsExpectedResult(decimal totalAmount, bool shouldBeValid)
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: false,
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Test Product", 1, totalAmount)
            }
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        if (shouldBeValid)
        {
            result.ShouldNotHaveAnyValidationErrors();
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Items);
        }
    }

    [Fact]
    public void Validate_ItemWithZeroQuantity_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: false,
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Product", 0, 500m) // Zero quantity
            }
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("Quantity must be greater than 0");
    }

    [Fact]
    public void Validate_ItemWithNegativePrice_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            IsVip: false,
            Items: new List<CreateOrderItemDto>
            {
                new(Guid.NewGuid(), "Product", 1, -100m) // Negative price
            }
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor("Items[0].UnitPrice")
            .WithErrorMessage("Unit price must be greater than 0");
    }
}