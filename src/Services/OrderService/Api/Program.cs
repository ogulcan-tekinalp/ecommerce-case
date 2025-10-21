using FluentValidation;
using MediatR;
using OrderService.Application.Orders.CreateOrder;
using OrderService.Application;
using OrderService.Infrastructure;
using OrderService.Api.Middleware;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOrderServiceApplication();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseMiddleware<ErrorHandlingMiddleware>(); 

app.MapControllers();
app.Run();

