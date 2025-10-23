namespace OrderService.Application.Orders.CreateOrder;

public record CreateOrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);