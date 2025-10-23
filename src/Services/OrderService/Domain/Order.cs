namespace OrderService.Domain.Entities;

using OrderService.Domain.Enums;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    
    // Navigation property for order items
    public List<OrderItem> Items { get; set; } = new();

    public bool IsVip { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? StockReservationId { get; set; }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be confirmed");
        
        Status = OrderStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Delivered || Status == OrderStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel order in {Status} status");

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        CancellationReason = reason;
    }

    public bool CanBeCancelled()
    {
        if (Status == OrderStatus.Delivered || Status == OrderStatus.Cancelled)
            return false;

        // Business rule: Can only cancel within 2 hours
        return DateTime.UtcNow - CreatedAtUtc <= TimeSpan.FromHours(2);
    }
}