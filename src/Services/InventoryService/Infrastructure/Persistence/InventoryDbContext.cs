namespace InventoryService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using InventoryService.Domain.Entities;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

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

        base.OnModelCreating(modelBuilder);
    }
}