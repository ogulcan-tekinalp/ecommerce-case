using MediatR;
using BuildingBlocks.Messaging;
using PaymentService.Application;
using PaymentService.Application.ProcessPayment;
using PaymentService.Application.EventHandlers;
using PaymentService.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/paymentservice-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.WithProperty("Service", "PaymentService")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();



var rabbitMqConnection = builder.Configuration.GetConnectionString("RabbitMQ") 
    ?? "amqp://guest:guest@localhost:5672";
builder.Services.AddRabbitMqMessageBus(rabbitMqConnection);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
.AddRabbitMQ(rabbitConnectionString: rabbitMqConnection);

// Add Application Services
builder.Services.AddPaymentServiceApplication();

// Add Infrastructure Services
builder.Services.AddPaymentServiceInfrastructure(builder.Configuration);

builder.Services.AddSingleton<StockReservedEventHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.MapHealthChecks("/health");

app.MapControllers();

var eventHandler = app.Services.GetRequiredService<StockReservedEventHandler>();

app.Logger.LogInformation("✅ PaymentService started - Event handler initialized");

app.Run();
Log.CloseAndFlush();