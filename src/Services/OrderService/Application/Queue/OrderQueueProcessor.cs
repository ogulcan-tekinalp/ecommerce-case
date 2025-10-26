using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Application.Sagas;

namespace OrderService.Application.Queue;

public class OrderQueueProcessor : BackgroundService
{
    private readonly OrderPriorityQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderQueueProcessor> _logger;

    public OrderQueueProcessor(
        OrderPriorityQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderQueueProcessor> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Order Queue Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Process orders from priority queue
                if (_queue.TryDequeue(out var orderId, out var isVip))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var saga = scope.ServiceProvider.GetRequiredService<OrderSaga>();

                    if (isVip)
                    {
                        _logger.LogInformation("⭐ Processing VIP order {OrderId} with priority", orderId);
                    }
                    else
                    {
                        _logger.LogInformation("📦 Processing regular order {OrderId}", orderId);
                    }

                    await saga.StartOrderFlowAsync(orderId, stoppingToken);
                }
                else
                {
                    // No orders in queue, wait a bit
                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing order from queue");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("🛑 Order Queue Processor stopped");
    }
}
