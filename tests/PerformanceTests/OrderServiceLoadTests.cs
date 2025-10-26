using NBomber;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;
using FluentAssertions;
using System.Net.Http.Json;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Messaging;

namespace PerformanceTests;

public class OrderServiceLoadTests : IDisposable
{
    private WebApplicationFactory<OrderService.Api.Program> _factory = null!;
    private HttpClient _client = null!;

    [Fact]
    public void OrderService_LoadTest_ShouldHandleConcurrentRequests()
    {
        _factory = new WebApplicationFactory<OrderService.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace RabbitMQ with InMemory message bus for performance tests
                    services.AddSingleton<IMessageBus, InMemoryMessageBus>();
                });
            });
        _client = _factory.CreateClient();

        var scenario = Scenario.Create("order_service_load_test", async context =>
        {
            var step1 = Step.Run("create_order", context, async () =>
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
                    },
                    IdempotencyKey = $"perf-test-{Guid.NewGuid()}"
                };

                var response = await _client.PostAsJsonAsync("/api/v1/orders", orderRequest);
                
                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            });

            var step2 = Step.Run("get_order", context, async () =>
            {
                var orderId = Guid.NewGuid();
                var response = await _client.GetAsync($"/api/v1/orders/{orderId}");
                
                return Response.Ok();
            });

            var step3 = Step.Run("health_check", context, async () =>
            {
                var response = await _client.GetAsync("/health");
                
                return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
            });

            return Response.Ok();
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromSeconds(30))
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
