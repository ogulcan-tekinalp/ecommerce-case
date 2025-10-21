using MediatR;

namespace OrderService.Application.Orders.CreateOrder;

public sealed record CreateOrderCommand(Guid CustomerId, decimal TotalAmount) : IRequest<Guid>;
