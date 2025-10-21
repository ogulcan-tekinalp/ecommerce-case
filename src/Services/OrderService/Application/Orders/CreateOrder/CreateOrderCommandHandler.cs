using MediatR;
using OrderService.Application.Abstractions;
using OrderService.Domain.Entities;

namespace OrderService.Application.Orders.CreateOrder;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _repo;
    public CreateOrderCommandHandler(IOrderRepository repo) => _repo = repo;

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = new Order
        {
            CustomerId = request.CustomerId,
            TotalAmount = request.TotalAmount
        };

        await _repo.AddAsync(order, ct);
        await _repo.SaveChangesAsync(ct);
        return order.Id;
    }
}
