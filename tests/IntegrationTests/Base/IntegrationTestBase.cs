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
using BuildingBlocks.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace IntegrationTests.Base;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected WebApplicationFactory<OrderService.Api.Program> OrderServiceFactory { get; private set; } = null!;
    protected WebApplicationFactory<InventoryService.Api.Program> InventoryServiceFactory { get; private set; } = null!;
    protected WebApplicationFactory<PaymentService.Api.Program> PaymentServiceFactory { get; private set; } = null!;
    
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

    private WebApplicationFactory<OrderService.Api.Program> CreateOrderServiceFactory()
    {
        return new WebApplicationFactory<OrderService.Api.Program>()
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

                    // Replace RabbitMQ connection - remove existing and add new with test connection
                    var messageBusDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageBus));
                    if (messageBusDescriptor != null)
                        services.Remove(messageBusDescriptor);
                    
                    services.AddRabbitMqMessageBus(RabbitMqContainer.GetConnectionString());
                    
                    // Replace health checks with test container connections
                    services.Configure<HealthCheckServiceOptions>(options =>
                    {
                        options.Registrations.Clear();
                    });
                    services.AddHealthChecks()
                        .AddNpgSql(PostgreSqlContainer.GetConnectionString())
                        .AddRabbitMQ(rabbitConnectionString: RabbitMqContainer.GetConnectionString());
                    
                    // Initialize database after service registration
                    var serviceProvider = services.BuildServiceProvider();
                    using var scope = serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                    dbContext.Database.EnsureCreated();
                });
            });
    }

    private WebApplicationFactory<InventoryService.Api.Program> CreateInventoryServiceFactory()
    {
        return new WebApplicationFactory<InventoryService.Api.Program>()
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

                    // Replace RabbitMQ connection - remove existing and add new with test connection
                    var messageBusDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageBus));
                    if (messageBusDescriptor != null)
                        services.Remove(messageBusDescriptor);
                    
                    services.AddRabbitMqMessageBus(RabbitMqContainer.GetConnectionString());
                    
                    // Replace health checks with test container connections
                    services.Configure<HealthCheckServiceOptions>(options =>
                    {
                        options.Registrations.Clear();
                    });
                    services.AddHealthChecks()
                        .AddNpgSql(PostgreSqlContainer.GetConnectionString())
                        .AddRabbitMQ(rabbitConnectionString: RabbitMqContainer.GetConnectionString());
                    
                    // Initialize database after service registration
                    var serviceProvider = services.BuildServiceProvider();
                    using var scope = serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                    dbContext.Database.EnsureCreated();
                });
            });
    }

    private WebApplicationFactory<PaymentService.Api.Program> CreatePaymentServiceFactory()
    {
        return new WebApplicationFactory<PaymentService.Api.Program>()
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

                    // Replace RabbitMQ connection - remove existing and add new with test connection
                    var messageBusDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMessageBus));
                    if (messageBusDescriptor != null)
                        services.Remove(messageBusDescriptor);
                    
                    services.AddRabbitMqMessageBus(RabbitMqContainer.GetConnectionString());
                    
                    // Replace health checks with test container connections
                    services.Configure<HealthCheckServiceOptions>(options =>
                    {
                        options.Registrations.Clear();
                    });
                    services.AddHealthChecks()
                        .AddNpgSql(PostgreSqlContainer.GetConnectionString())
                        .AddRabbitMQ(rabbitConnectionString: RabbitMqContainer.GetConnectionString());
                    
                    // Initialize database after service registration
                    var serviceProvider = services.BuildServiceProvider();
                    using var scope = serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
                    dbContext.Database.EnsureCreated();
                });
            });
    }
}
