using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Common.Behaviors;
using OrderService.Application.Orders.CreateOrder;
using OrderService.Application.Vip;
using OrderService.Application.BackgroundJobs;

namespace OrderService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderServiceApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<CreateOrderCommand>());

        services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

        // 🔑 MediatR pipeline'a FluentValidation Behavior'u ekliyoruz
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // VIP Order Processing Service
        services.AddScoped<VipOrderProcessingService>();
        
        // VIP Order Processing Background Service
        services.AddHostedService<VipOrderProcessingBackgroundService>();

        return services;
    }
}
