namespace OrderService.Domain.Entities;

using OrderService.Domain.Enums;

public class OrderEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
