using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Messaging;

public static class RabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqMessageBus(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton<IMessageBus>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RabbitMqMessageBus>>();
            return new RabbitMqMessageBus(connectionString, logger);
        });

        return services;
    }
}