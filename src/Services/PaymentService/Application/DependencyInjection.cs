using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Abstractions;
using PaymentService.Application.FraudDetection;
using PaymentService.Application.ProcessPayment;
using PaymentService.Application.RefundPayment;

namespace PaymentService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentServiceApplication(this IServiceCollection services)
    {
        // Register application services
        services.AddScoped<PaymentProcessor>();
        services.AddScoped<IFraudDetectionService, FraudDetectionService>();

        // Register MediatR handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        return services;
    }
}
