using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace OrderService.Application.Queue;

public class OrderPriorityQueue
{
    private readonly ConcurrentQueue<Guid> _vipQueue = new();
    private readonly ConcurrentQueue<Guid> _regularQueue = new();
    private readonly ILogger<OrderPriorityQueue> _logger;

    public OrderPriorityQueue(ILogger<OrderPriorityQueue> logger)
    {
        _logger = logger;
    }

    public void EnqueueVip(Guid orderId)
    {
        _vipQueue.Enqueue(orderId);
        _logger.LogInformation("⭐ VIP order {OrderId} added to priority queue", orderId);
    }

    public void EnqueueRegular(Guid orderId)
    {
        _regularQueue.Enqueue(orderId);
        _logger.LogInformation("📦 Regular order {OrderId} added to queue", orderId);
    }

    public bool TryDequeue(out Guid orderId, out bool isVip)
    {
        // Always prioritize VIP orders first
        if (_vipQueue.TryDequeue(out orderId))
        {
            isVip = true;
            _logger.LogInformation("⚡ Processing VIP order {OrderId} from priority queue", orderId);
            return true;
        }

        // Process regular orders only if no VIP orders waiting
        if (_regularQueue.TryDequeue(out orderId))
        {
            isVip = false;
            _logger.LogInformation("📋 Processing regular order {OrderId} from queue", orderId);
            return true;
        }

        orderId = Guid.Empty;
        isVip = false;
        return false;
    }

    public int VipQueueCount => _vipQueue.Count;
    public int RegularQueueCount => _regularQueue.Count;
    public int TotalQueueCount => VipQueueCount + RegularQueueCount;

    public QueueStatus GetStatus()
    {
        return new QueueStatus(VipQueueCount, RegularQueueCount, TotalQueueCount);
    }
}

public record QueueStatus(int VipCount, int RegularCount, int TotalCount);
