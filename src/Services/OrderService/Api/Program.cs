using FluentValidation;
using MediatR;
using OrderService.Application.Orders.CreateOrder;
using OrderService.Application;
using OrderService.Infrastructure;
using OrderService.Api.Middleware;
using BuildingBlocks.Messaging;
using OrderService.Application.Sagas;

var builder = WebApplication.CreateBuilder(args);

// ⚡ Add Message Bus
builder.Services.AddInMemoryMessageBus();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddTransient<ErrorHandlingMiddleware>();

// MediatR v12
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<CreateOrderCommand>());

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

// Infrastructure (DbContext + Repo)
builder.Services.AddOrderServiceInfrastructure(builder.Configuration);

// Application layer
builder.Services.AddOrderServiceApplication();

// ⚡ Register Order Saga and Simulator as Singleton
builder.Services.AddSingleton<OrderSaga>();
builder.Services.AddSingleton<StockPaymentSimulator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseMiddleware<ErrorHandlingMiddleware>(); 
app.MapControllers();

// ⚡ CRITICAL: Initialize simulator FIRST (so it subscribes to OrderCreatedEvent)
var simulator = app.Services.GetRequiredService<StockPaymentSimulator>();
// ⚡ Then initialize saga
var saga = app.Services.GetRequiredService<OrderSaga>();

app.Logger.LogInformation("✅ Order Saga initialized and subscribed to events");
app.Logger.LogInformation("✅ Stock & Payment Simulator initialized (TEST MODE)");

app.Run();