namespace OrderService.Application.Orders.CancelOrder;

using MediatR;

public sealed record CancelOrderCommand(Guid OrderId, string? Reason) : IRequest<bool>;