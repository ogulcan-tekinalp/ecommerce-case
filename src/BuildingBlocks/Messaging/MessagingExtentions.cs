namespace BuildingBlocks.Messaging;

using Microsoft.Extensions.DependencyInjection;

public static class MessagingExtensions
{
    public static IServiceCollection AddInMemoryMessageBus(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBus, InMemoryMessageBus>();
        return services;
    }
}