using NBomber;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;
using FluentAssertions;

namespace PerformanceTests;

public class OrderServiceLoadTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [Fact]
    public void OrderService_LoadTest_ShouldHandleConcurrentRequests()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();

        var scenario = Scenario.Create("order_service_load_test", async context =>
        {
            var step1 = await Step.Run("create_order", async context =>
            {
                var orderRequest = new
                {
                    CustomerId = Guid.NewGuid(),
                    Items = new[]
                    {
                        new
                        {
                            ProductId = Guid.NewGuid(),
                            ProductName = "Test Product",
                            Quantity = 1,
                            UnitPrice = 100.0m
                        }
                    }
                };

                var response = await _client.PostAsJsonAsync("/api/v1/orders", orderRequest);
                
                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            });

            var step2 = await Step.Run("get_order", async context =>
            {
                var orderId = Guid.NewGuid();
                var response = await _client.GetAsync($"/api/v1/orders/{orderId}");
                
                return Response.Ok();
            });

            var step3 = await Step.Run("health_check", async context =>
            {
                var response = await _client.GetAsync("/health");
                
                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            });

            return Response.Ok();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromMinutes(1))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assertions
        stats.AllOkCount.Should().BeGreaterThan(0);
        stats.AllFailCount.Should().BeLessThan(stats.AllOkCount);
        
        // Performance assertions
        stats.AllOkCount.Should().BeGreaterThan(500); // At least 500 successful requests
        stats.AllFailCount.Should().BeLessThan(50); // Less than 50 failed requests
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
