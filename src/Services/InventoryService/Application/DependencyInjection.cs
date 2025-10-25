using Microsoft.Extensions.DependencyInjection;
using InventoryService.Application.Inventory.LowStockAlert;
using InventoryService.Application.BackgroundJobs;

namespace InventoryService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryServiceApplication(this IServiceCollection services)
    {
        // Register application services
        services.AddScoped<ILowStockAlertService, LowStockAlertService>();
        services.AddHostedService<LowStockAlertBackgroundService>();

        // Register MediatR handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        return services;
    }
}
