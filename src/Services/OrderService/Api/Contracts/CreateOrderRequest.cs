namespace OrderService.Api.Contracts;

public sealed record CreateOrderRequest(
    Guid CustomerId,
    bool IsVip,
    List<CreateOrderItemRequest> Items
);

public record CreateOrderItemRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);