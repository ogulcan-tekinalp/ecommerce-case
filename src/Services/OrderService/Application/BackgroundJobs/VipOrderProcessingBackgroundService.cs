using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Application.Vip;

namespace OrderService.Application.BackgroundJobs;

public class VipOrderProcessingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VipOrderProcessingBackgroundService> _logger;

    public VipOrderProcessingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<VipOrderProcessingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 VIP Order Processing Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var vipService = scope.ServiceProvider.GetRequiredService<VipOrderProcessingService>();
                var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

                // Get pending VIP orders
                var pendingVipOrders = await orderRepository.GetPendingVipOrdersAsync(stoppingToken);
                
                if (pendingVipOrders.Any())
                {
                    _logger.LogInformation("🎯 Processing {Count} pending VIP orders", pendingVipOrders.Count);
                    
                    foreach (var order in pendingVipOrders)
                    {
                        await vipService.ProcessVipOrderAsync(order.Id, stoppingToken);
                    }
                }

                // Wait 30 seconds before next check
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in VIP Order Processing Background Service");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        _logger.LogInformation("🛑 VIP Order Processing Background Service stopped");
    }
}
