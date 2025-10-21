using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderService.Infrastructure.Persistence;

public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        // 1) Env var'ı oku (CI/CD ya da lokal için pratik)
        var conn = Environment.GetEnvironmentVariable("ORDER_DB")
            ?? "Host=localhost;Port=5432;Database=ecommerce;Username=app;Password=app";

        // 2) Options oluştur
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(conn)
            .Options;

        // 3) Parametreli ctor ile dön
        return new OrderDbContext(options);
    }
}
