namespace InventoryService.Domain.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int TotalQuantity => AvailableQuantity + ReservedQuantity;
    public decimal Price { get; set; }
    
    // Optimistic locking
    public uint Version { get; set; }
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    // Business logic
    public bool CanReserve(int quantity)
    {
        return AvailableQuantity >= quantity;
    }

    public void Reserve(int quantity)
    {
        if (!CanReserve(quantity))
            throw new InvalidOperationException($"Insufficient stock. Available: {AvailableQuantity}, Requested: {quantity}");

        AvailableQuantity -= quantity;
        ReservedQuantity += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Release(int quantity)
    {
        if (ReservedQuantity < quantity)
            throw new InvalidOperationException($"Cannot release more than reserved. Reserved: {ReservedQuantity}, Requested: {quantity}");

        ReservedQuantity -= quantity;
        AvailableQuantity += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsLowStock() => AvailableQuantity <= 10;
}