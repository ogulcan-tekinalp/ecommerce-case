using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Abstractions;
using OrderService.Application.Services;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Services;

namespace OrderService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderServiceInfrastructure(
        this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<OrderDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("Default")));

        services.AddScoped<IOrderRepository, OrderRepository>();

        // HTTP Client for InventoryService
        services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>(client =>
        {
            var baseUrl = cfg["ServiceUrls:InventoryService"] ?? "http://localhost:5207";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
