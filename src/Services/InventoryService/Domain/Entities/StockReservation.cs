namespace InventoryService.Domain.Entities;

public class StockReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime ReservedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } // 10 dakika sonra
    public bool IsReleased { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
    public string? ReleaseReason { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;

    public bool IsExpired() => DateTime.UtcNow > ExpiresAtUtc && !IsReleased;

    public void Release(string reason)
    {
        if (IsReleased)
            throw new InvalidOperationException("Reservation already released");

        IsReleased = true;
        ReleasedAtUtc = DateTime.UtcNow;
        ReleaseReason = reason;
    }
}