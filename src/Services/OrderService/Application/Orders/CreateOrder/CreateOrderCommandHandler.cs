namespace OrderService.Application.Orders.CreateOrder;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Application.Queue;
using OrderService.Domain.Entities;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _repo;
    private readonly OrderPriorityQueue _priorityQueue;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    
    public CreateOrderCommandHandler(
        IOrderRepository repo,
        OrderPriorityQueue priorityQueue,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _repo = repo;
        _priorityQueue = priorityQueue;
        _logger = logger;
    }
    
    public async Task<Guid> Handle(CreateOrderCommand req, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(req.IdempotencyKey))
        {
            var existingOrder = await _repo.GetByIdempotencyKeyAsync(req.IdempotencyKey, ct);
            if (existingOrder != null)
            {
                _logger.LogInformation("Duplicate order detected with IdempotencyKey: {Key}, returning existing OrderId: {OrderId}",
                    req.IdempotencyKey, existingOrder.Id);
                return existingOrder.Id;
            }
        }
        
        var order = new Order
        {
            CustomerId = req.CustomerId,
            IsVip = req.IsVip,
            TotalAmount = req.Items.Sum(i => i.Quantity * i.UnitPrice),
            Items = req.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
        
        if (!string.IsNullOrEmpty(req.IdempotencyKey))
        {
            order.SetIdempotencyKey(req.IdempotencyKey);
        }
        
        await _repo.AddAsync(order, ct);
        await _repo.SaveChangesAsync(ct);
        
        _logger.LogInformation("Order {OrderId} created for Customer {CustomerId}, Total: {Total}",
            order.Id, order.CustomerId, order.TotalAmount);
        
        // Add order to priority queue based on VIP status
        if (order.IsVip)
        {
            _priorityQueue.EnqueueVip(order.Id);
            _logger.LogInformation("⭐ VIP Order {OrderId} added to priority queue", order.Id);
        }
        else
        {
            _priorityQueue.EnqueueRegular(order.Id);
            _logger.LogInformation("📦 Regular Order {OrderId} added to queue", order.Id);
        }
        
        return order.Id;
    }
}