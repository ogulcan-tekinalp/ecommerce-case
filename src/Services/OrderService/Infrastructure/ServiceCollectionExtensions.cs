using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Abstractions;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrderServiceInfrastructure(
        this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<OrderDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("Default")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }
}
