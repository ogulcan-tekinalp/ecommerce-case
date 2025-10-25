namespace InventoryService.Domain.Entities;

public class FlashSaleProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int MaxQuantityPerCustomer { get; set; } = 2; // Default limit
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Product? Product { get; set; }

    public bool IsCurrentlyActive()
    {
        var now = DateTime.UtcNow;
        return IsActive && now >= StartTimeUtc && now <= EndTimeUtc;
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow > EndTimeUtc;
    }
}
