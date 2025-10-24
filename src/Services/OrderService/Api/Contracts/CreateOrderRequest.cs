namespace OrderService.Api.Contracts;

public record CreateOrderRequest(
    Guid CustomerId,
    bool IsVip,
    List<CreateOrderItemRequest> Items,
    string? IdempotencyKey = null
);

public record CreateOrderItemRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);