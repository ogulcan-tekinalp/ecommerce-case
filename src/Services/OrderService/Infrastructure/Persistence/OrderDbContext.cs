using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) {}

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("orders");
            b.HasKey(x => x.Id);
            b.Property(x => x.CustomerId).IsRequired();
            b.Property(x => x.TotalAmount).HasColumnType("numeric(18,2)");
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
        });
        base.OnModelCreating(modelBuilder);
    }
}
