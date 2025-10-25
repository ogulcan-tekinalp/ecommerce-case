using HotChocolate;
using HotChocolate.Types;
using BuildingBlocks.GraphQL.Types;
using OrderService.Application.Abstractions;
using OrderService.Domain;

namespace BuildingBlocks.GraphQL.Queries;

[ExtendObjectType("Query")]
public class OrderQueries
{
    public async Task<OrderType?> GetOrderAsync(
        Guid id,
        [Service] IOrderRepository orderRepository,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken);
        
        if (order == null)
            return null;

        return new OrderType
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            Items = order.Items.Select(item => new OrderItemType
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList()
        };
    }

    public async Task<List<OrderType>> GetOrdersByCustomerAsync(
        Guid customerId,
        [Service] IOrderRepository orderRepository,
        CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        
        return orders.Select(order => new OrderType
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            Items = order.Items.Select(item => new OrderItemType
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList()
        }).ToList();
    }

    public async Task<List<OrderType>> GetAllOrdersAsync(
        [Service] IOrderRepository orderRepository,
        CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.GetAllAsync(cancellationToken);
        
        return orders.Select(order => new OrderType
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            Items = order.Items.Select(item => new OrderItemType
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList()
        }).ToList();
    }
}
