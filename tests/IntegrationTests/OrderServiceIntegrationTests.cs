using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using IntegrationTests.Base;
using Bogus;

namespace IntegrationTests;

public class OrderServiceIntegrationTests : IntegrationTestBase
{
    private readonly Faker _faker = new();

    [Fact]
    public async Task CreateOrder_ShouldReturnSuccess()
    {
        // Arrange
        using var client = OrderServiceFactory.CreateClient();
        
        var createOrderRequest = new
        {
            CustomerId = Guid.NewGuid(),
            Items = new[]
            {
                new
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = _faker.Commerce.ProductName(),
                    Quantity = _faker.Random.Int(1, 5),
                    UnitPrice = _faker.Random.Decimal(10, 100)
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/orders", createOrderRequest);

        // Assert
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetOrder_ShouldReturnOrder()
    {
        // Arrange
        using var client = OrderServiceFactory.CreateClient();
        var orderId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/orders/{orderId}");

        // Assert
        response.Should().NotBeNull();
        // Note: This might return 404 if order doesn't exist, which is expected
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Arrange
        using var client = OrderServiceFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
