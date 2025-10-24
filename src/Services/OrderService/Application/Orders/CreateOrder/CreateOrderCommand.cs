namespace OrderService.Application.Orders.CreateOrder;
using MediatR;

public sealed record CreateOrderCommand(
    Guid CustomerId,
    bool IsVip,
    List<CreateOrderItemDto> Items,
    string? IdempotencyKey = null
) : IRequest<Guid>;