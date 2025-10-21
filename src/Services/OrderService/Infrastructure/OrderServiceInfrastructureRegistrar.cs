using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OrderService.Infrastructure;

public static class OrderServiceInfrastructureRegistrar
{
    public static IServiceCollection AddOrderInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: EF Core DbContext ve diğer kayıtlar bir sonraki adımda gelecek.
        return services;
    }
}
