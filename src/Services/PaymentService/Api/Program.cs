using MediatR;
using BuildingBlocks.Messaging;
using PaymentService.Application;
using PaymentService.Application.ProcessPayment;
using PaymentService.Application.EventHandlers;

var builder = WebApplication.CreateBuilder(args);

var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMQ") 
    ?? "amqp://guest:guest@localhost:5672";
builder.Services.AddRabbitMqMessageBus(rabbitMqConnection);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<ProcessPaymentCommand>());

builder.Services.AddSingleton<PaymentProcessor>();
builder.Services.AddSingleton<StockReservedEventHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.MapControllers();

var eventHandler = app.Services.GetRequiredService<StockReservedEventHandler>();

app.Logger.LogInformation("✅ PaymentService started - Event handler initialized");

app.Run();