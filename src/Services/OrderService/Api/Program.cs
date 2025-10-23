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

// ⚡ Register Order Saga as Singleton (keeps event subscriptions alive)
builder.Services.AddSingleton<OrderSaga>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseMiddleware<ErrorHandlingMiddleware>(); 
app.MapControllers();

// ⚡ Initialize the saga (this activates event subscriptions)
var saga = app.Services.GetRequiredService<OrderSaga>();
app.Logger.LogInformation("Order Saga initialized and subscribed to events");

app.Run();