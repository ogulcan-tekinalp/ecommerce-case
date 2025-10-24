using MediatR;

namespace OrderService.Application.Orders.GetOrder;

public record GetOrderQuery(Guid OrderId) : IRequest<GetOrderResult?>;

public record GetOrderResult(
    Guid Id,
    Guid CustomerId,
    decimal TotalAmount,
    string Status,
    bool IsVip,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    string? CancellationReason,
    Guid? StockReservationId,
    Guid? PaymentId,
    List<OrderItemDto> Items
);

public record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);