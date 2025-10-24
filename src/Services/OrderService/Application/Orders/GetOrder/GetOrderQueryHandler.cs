using MediatR;
using OrderService.Application.Abstractions;

namespace OrderService.Application.Orders.GetOrder;

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, GetOrderResult?>
{
    private readonly IOrderRepository _repository;

    public GetOrderQueryHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetOrderResult?> Handle(GetOrderQuery request, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(request.OrderId, ct);
        
        if (order == null)
            return null;

        return new GetOrderResult(
            Id: order.Id,
            CustomerId: order.CustomerId,
            TotalAmount: order.TotalAmount,
            Status: order.Status.ToString(),
            IsVip: order.IsVip,
            CreatedAt: order.CreatedAtUtc,
            ConfirmedAt: order.ConfirmedAtUtc,
            CancelledAt: order.CancelledAtUtc,
            CancellationReason: order.CancellationReason,
            StockReservationId: order.StockReservationId,
            PaymentId: order.PaymentId,
            Items: order.Items.Select(i => new OrderItemDto(
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice
            )).ToList()
        );
    }
}