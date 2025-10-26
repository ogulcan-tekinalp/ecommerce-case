using FluentValidation;
using MediatR;
using OrderService.Application.Orders.CreateOrder;
using OrderService.Application;
using OrderService.Infrastructure;
using OrderService.Api.Middleware;
using BuildingBlocks.Messaging;
using OrderService.Application.Sagas;
using Serilog;
using BuildingBlocks.Observability;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/orderservice-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.WithProperty("Service", "OrderService")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// ⚡ Add Message Bus
var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMQ")
    ?? "amqp://guest:guest@localhost:5672";
    
builder.Services.AddRabbitMqMessageBus(rabbitMqConnection);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddTransient<ErrorHandlingMiddleware>();
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!)
    .AddRabbitMQ(rabbitConnectionString: rabbitMqConnection);

// MediatR v12
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<CreateOrderCommand>());

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

// Infrastructure (DbContext + Repo)
builder.Services.AddOrderServiceInfrastructure(builder.Configuration);

// Application layer
builder.Services.AddOrderServiceApplication();

// ⚡ Saga and Simulator are registered in Application layer
builder.Host.UseSerilog();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCorrelationId(); // Add correlation ID middleware
app.UseMiddleware<ErrorHandlingMiddleware>(); 
app.MapHealthChecks("/health");

app.MapControllers();

// ⚡ Then initialize saga and simulator
var saga = app.Services.GetRequiredService<OrderSaga>();
var simulator = app.Services.GetRequiredService<StockPaymentSimulator>();
simulator.Initialize();

app.Logger.LogInformation("✅ Order Saga initialized and subscribed to events");
app.Logger.LogInformation("✅ StockPaymentSimulator initialized - Payment simulation active");

app.Run();

Log.CloseAndFlush();

// Make Program class accessible for integration tests
namespace OrderService.Api
{
    public partial class Program { }
}