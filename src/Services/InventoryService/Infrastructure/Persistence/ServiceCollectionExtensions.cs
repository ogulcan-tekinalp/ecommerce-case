namespace InventoryService.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using InventoryService.Application.Abstractions;
using InventoryService.Infrastructure.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryServiceInfrastructure(
        this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<InventoryDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("Default")));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockReservationRepository, StockReservationRepository>();
        services.AddScoped<IFlashSaleRepository, FlashSaleRepository>();
        services.AddScoped<ICustomerPurchaseRepository, CustomerPurchaseRepository>();
        
        return services;
    }
}