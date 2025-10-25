namespace InventoryService.Domain.Entities;

public class CustomerPurchase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? FlashSaleProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime PurchaseDateUtc { get; set; } = DateTime.UtcNow;
    public Guid? OrderId { get; set; }

    // Navigation properties
    public Product? Product { get; set; }
    public FlashSaleProduct? FlashSaleProduct { get; set; }
}
