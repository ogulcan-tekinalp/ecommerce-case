namespace OrderService.Api.Contracts;

public sealed record CreateOrderRequest(Guid CustomerId, decimal TotalAmount);
