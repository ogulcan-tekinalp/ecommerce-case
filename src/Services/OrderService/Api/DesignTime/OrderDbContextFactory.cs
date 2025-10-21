using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Api.DesignTime;

public class OrderDbContextFactory : IDesignTimeDbContextFactory<OrderDbContext>
{
    public OrderDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ORDER_DB")
            ?? "Host=localhost;Port=5432;Database=ecommerce;Username=app;Password=app";

        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseNpgsql(conn)
            .Options;

        return new OrderDbContext(options);
    }
}
