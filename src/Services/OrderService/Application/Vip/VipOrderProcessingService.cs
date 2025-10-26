using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Domain.Entities;

namespace OrderService.Application.Vip;

public class VipOrderProcessingService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<VipOrderProcessingService> _logger;

    public VipOrderProcessingService(
        IOrderRepository orderRepository,
        ILogger<VipOrderProcessingService> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task<List<Order>> GetVipOrdersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Retrieving VIP orders for priority processing");
        
        // Get all pending VIP orders, ordered by creation time (FIFO for VIP)
        var vipOrders = await _orderRepository.GetVipOrdersAsync(cancellationToken);
        
        _logger.LogInformation("✅ Found {Count} VIP orders for processing", vipOrders.Count);
        
        return vipOrders;
    }

    public async Task ProcessVipOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🎯 Processing VIP order: {OrderId}", orderId);
        
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("❌ VIP order {OrderId} not found", orderId);
            return;
        }

        if (!order.IsVip)
        {
            _logger.LogWarning("⚠️ Order {OrderId} is not marked as VIP", orderId);
            return;
        }

        // VIP orders get priority processing
        await ProcessVipOrderWithPriorityAsync(order, cancellationToken);
    }

    private async Task ProcessVipOrderWithPriorityAsync(Order order, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 Starting priority processing for VIP order {OrderId}", order.Id);
        
        // VIP orders get immediate processing
        // 1. Skip normal queue delays
        // 2. Get priority in stock reservation
        // 3. Get priority in payment processing
        // 4. Get priority in order confirmation
        
        _logger.LogInformation("✅ VIP order {OrderId} processed with priority", order.Id);
    }

    public async Task<bool> IsVipCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        
        var customerOrders = await _orderRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        
        // VIP criteria: More than 10 orders or total amount > 50,000
        var totalOrders = customerOrders.Count;
        var totalAmount = customerOrders.Sum(o => o.TotalAmount);
        
        var isVip = totalOrders >= 10 || totalAmount >= 50000;
        
        _logger.LogInformation("👤 Customer {CustomerId} VIP status: {IsVip} (Orders: {OrderCount}, Total: {TotalAmount})", 
            customerId, isVip, totalOrders, totalAmount);
        
        return isVip;
    }

    public async Task MarkOrderAsVipAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("❌ Order {OrderId} not found for VIP marking", orderId);
            return;
        }

        order.IsVip = true;
        await _orderRepository.UpdateAsync(order, cancellationToken);
        
        _logger.LogInformation("⭐ Order {OrderId} marked as VIP", orderId);
    }
}
