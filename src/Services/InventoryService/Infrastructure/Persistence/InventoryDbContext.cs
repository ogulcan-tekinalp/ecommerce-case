namespace InventoryService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using InventoryService.Domain.Entities;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<FlashSaleProduct> FlashSaleProducts => Set<FlashSaleProduct>();
    public DbSet<CustomerPurchase> CustomerPurchases => Set<CustomerPurchase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("products");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.AvailableQuantity).IsRequired();
            b.Property(x => x.ReservedQuantity).IsRequired();
            b.Property(x => x.Price).HasColumnType("numeric(18,2)");
            
            // ⚡ Optimistic Locking
            b.Property(x => x.Version)
                .IsRowVersion()
                .IsConcurrencyToken();
            
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc);
        });

        modelBuilder.Entity<StockReservation>(b =>
        {
            b.ToTable("stock_reservations");
            b.HasKey(x => x.Id);
            b.Property(x => x.OrderId).IsRequired();
            b.Property(x => x.ProductId).IsRequired();
            b.Property(x => x.Quantity).IsRequired();
            b.Property(x => x.ReservedAtUtc).IsRequired();
            b.Property(x => x.ExpiresAtUtc).IsRequired();
            b.Property(x => x.IsReleased).IsRequired();
            b.Property(x => x.ReleaseReason).HasMaxLength(500);

            // Relationship
            b.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for performance
            b.HasIndex(x => x.OrderId);
            b.HasIndex(x => new { x.ExpiresAtUtc, x.IsReleased });
        });

        modelBuilder.Entity<FlashSaleProduct>(b =>
        {
            b.ToTable("flash_sale_products");
            b.HasKey(x => x.Id);
            b.Property(x => x.ProductId).IsRequired();
            b.Property(x => x.StartTimeUtc).IsRequired();
            b.Property(x => x.EndTimeUtc).IsRequired();
            b.Property(x => x.MaxQuantityPerCustomer).IsRequired();
            b.Property(x => x.IsActive).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();

            // Relationship
            b.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => new { x.IsActive, x.StartTimeUtc, x.EndTimeUtc });
        });

        modelBuilder.Entity<CustomerPurchase>(b =>
        {
            b.ToTable("customer_purchases");
            b.HasKey(x => x.Id);
            b.Property(x => x.CustomerId).IsRequired();
            b.Property(x => x.ProductId).IsRequired();
            b.Property(x => x.FlashSaleProductId);
            b.Property(x => x.Quantity).IsRequired();
            b.Property(x => x.PurchaseDateUtc).IsRequired();
            b.Property(x => x.OrderId);

            // Relationships
            b.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.FlashSaleProduct)
                .WithMany()
                .HasForeignKey(x => x.FlashSaleProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.FlashSaleProductId);
            b.HasIndex(x => new { x.CustomerId, x.ProductId });
            b.HasIndex(x => new { x.CustomerId, x.FlashSaleProductId });
        });

        base.OnModelCreating(modelBuilder);
    }
}