namespace OrderService.Application.Orders.RetryOrder;

using MediatR;

public sealed record RetryOrderCommand(Guid OrderId) : IRequest<bool>;