cd /Users/ogulcan/ecommerce-case/src/Services/PaymentService/Infrastructure
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Relational
# Eğer PostgreSQL kullanıyorsanız:
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
# Eğer SQL Server kullanıyorsanız:
# dotnet add package Microsoft.EntityFrameworkCore.SqlServer

dotnet restore
dotnet build
using PaymentService.Application;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(b =>
        {
            b.ToTable("payments");
            b.HasKey(x => x.Id);
            
            b.Property(x => x.OrderId).IsRequired();
            b.Property(x => x.CustomerId).IsRequired();
            b.Property(x => x.Amount)
                .HasColumnType("numeric(18,2)")
                .IsRequired();
            
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.Method).IsRequired();
            
            b.Property(x => x.TransactionId).HasMaxLength(50);
            b.Property(x => x.FailureReason).HasMaxLength(500);
            
            b.Property(x => x.IsFraudulent).IsRequired();
            b.Property(x => x.RetryCount).IsRequired();
            
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.ProcessedAtUtc);
            b.Property(x => x.RefundedAtUtc);

            // Indexes
            b.HasIndex(x => x.OrderId);
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.TransactionId);
            b.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        });

        base.OnModelCreating(modelBuilder);
    }
}