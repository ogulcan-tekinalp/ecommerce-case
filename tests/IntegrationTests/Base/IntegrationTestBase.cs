using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.Persistence;
using InventoryService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Persistence;

namespace IntegrationTests.Base;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected WebApplicationFactory<Program> OrderServiceFactory { get; private set; } = null!;
    protected WebApplicationFactory<Program> InventoryServiceFactory { get; private set; } = null!;
    protected WebApplicationFactory<Program> PaymentServiceFactory { get; private set; } = null!;
    
    protected PostgreSqlContainer PostgreSqlContainer { get; private set; } = null!;
    protected RabbitMqContainer RabbitMqContainer { get; private set; } = null!;
    protected RedisContainer RedisContainer { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Start containers
        PostgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("ecommerce_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithPortBinding(5432, true)
            .Build();

        RabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithPortBinding(5672, true)
            .WithPortBinding(15672, true)
            .Build();

        RedisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithPortBinding(6379, true)
            .Build();

        await Task.WhenAll(
            PostgreSqlContainer.StartAsync(),
            RabbitMqContainer.StartAsync(),
            RedisContainer.StartAsync()
        );

        // Create service factories
        OrderServiceFactory = CreateOrderServiceFactory();
        InventoryServiceFactory = CreateInventoryServiceFactory();
        PaymentServiceFactory = CreatePaymentServiceFactory();
    }

    public async Task DisposeAsync()
    {
        OrderServiceFactory?.Dispose();
        InventoryServiceFactory?.Dispose();
        PaymentServiceFactory?.Dispose();

        await Task.WhenAll(
            PostgreSqlContainer?.StopAsync() ?? Task.CompletedTask,
            RabbitMqContainer?.StopAsync() ?? Task.CompletedTask,
            RedisContainer?.StopAsync() ?? Task.CompletedTask
        );
    }

    private WebApplicationFactory<Program> CreateOrderServiceFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace database connection
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<OrderDbContext>(options =>
                        options.UseNpgsql(PostgreSqlContainer.GetConnectionString()));

                    // Replace RabbitMQ connection
                    services.Configure<RabbitMQOptions>(options =>
                    {
                        options.ConnectionString = RabbitMqContainer.GetConnectionString();
                    });
                });
            });
    }

    private WebApplicationFactory<Program> CreateInventoryServiceFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace database connection
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<InventoryDbContext>(options =>
                        options.UseNpgsql(PostgreSqlContainer.GetConnectionString()));
                });
            });
    }

    private WebApplicationFactory<Program> CreatePaymentServiceFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace database connection
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PaymentDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<PaymentDbContext>(options =>
                        options.UseNpgsql(PostgreSqlContainer.GetConnectionString()));
                });
            });
    }
}
