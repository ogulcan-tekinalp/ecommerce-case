using FluentValidation;
using MediatR;
using BuildingBlocks.Messaging;
using InventoryService.Application.Inventory.ReserveStock;
using InventoryService.Application.EventHandlers;
using InventoryService.Infrastructure;
using InventoryService.Infrastructure.Persistence;
using InventoryService.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add Message Bus
builder.Services.AddInMemoryMessageBus();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<ReserveStockCommand>());

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<ReserveStockCommandValidator>();

// Infrastructure (DbContext + Repositories)
builder.Services.AddInventoryServiceInfrastructure(builder.Configuration);

// Event Handlers (Singleton - keep subscriptions alive)
builder.Services.AddSingleton<OrderCreatedEventHandler>();
builder.Services.AddSingleton<StockReleasedEventHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.MapControllers();

// Initialize event handlers
var orderCreatedHandler = app.Services.GetRequiredService<OrderCreatedEventHandler>();
var stockReleasedHandler = app.Services.GetRequiredService<StockReleasedEventHandler>();

app.Logger.LogInformation("✅ InventoryService started - Event handlers initialized");
// Seed test products (Development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    
    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"), Name = "Laptop", Price = 15000, AvailableQuantity = 100 },
            new Product { Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440002"), Name = "Mouse", Price = 150, AvailableQuantity = 500 },
            new Product { Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440003"), Name = "Keyboard", Price = 500, AvailableQuantity = 200 }
        );
        await db.SaveChangesAsync();
        app.Logger.LogInformation("✅ Test products seeded");
    }
}
app.Run();