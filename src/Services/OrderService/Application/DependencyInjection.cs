using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Common.Behaviors;
using OrderService.Application.Orders.CreateOrder;

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

        return services;
    }
}
