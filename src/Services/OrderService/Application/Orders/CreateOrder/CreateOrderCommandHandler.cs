namespace OrderService.Application.Orders.CreateOrder;

using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Application.Sagas;
using OrderService.Domain.Entities;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _repo;
    private readonly OrderSaga _saga;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository repo,
        OrderSaga saga,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _repo = repo;
        _saga = saga;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateOrderCommand req, CancellationToken ct)
    {
        // Create order with items
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

        await _repo.AddAsync(order, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} created for Customer {CustomerId}, Total: {Total}",
            order.Id, order.CustomerId, order.TotalAmount);

        _logger.LogInformation("🚀 Starting saga for Order {OrderId}", order.Id);
        
        // Start the saga orchestration (fire-and-forget)
        _ = Task.Run(async () => await _saga.StartOrderFlowAsync(order.Id, ct), ct);

        return order.Id;
    }
}