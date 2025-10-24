namespace OrderService.Api.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Contracts;
using OrderService.Application.Abstractions;
using OrderService.Application.Orders.CreateOrder;
using OrderService.Application.Orders.CancelOrder;
using OrderService.Application.Orders.RetryOrder;


[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IOrderRepository _repo;

    public OrdersController(ISender mediator, IOrderRepository repo)
    {
        _mediator = mediator;
        _repo = repo;
    }
    

    [HttpPost]
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
{
    var items = request.Items.Select(i => new CreateOrderItemDto(
        i.ProductId,
        i.ProductName,
        i.Quantity,
        i.UnitPrice
    )).ToList();

    var command = new CreateOrderCommand(
        CustomerId: request.CustomerId,
        IsVip: request.IsVip,
        Items: items,
        IdempotencyKey: request.IdempotencyKey
    );

    var id = await _mediator.Send(command, ct);
    return CreatedAtAction(nameof(GetById), new { orderId = id }, new { orderId = id });
}

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid orderId, CancellationToken ct)
    {
        var order = await _repo.GetByIdAsync(orderId, ct);
        if (order is null) return NotFound();
        
        return Ok(new
        {
            order.Id,
            order.CustomerId,
            order.TotalAmount,
            Status = order.Status.ToString(),
            order.CreatedAtUtc,
            order.ConfirmedAtUtc,
            order.CancelledAtUtc,
            order.CancellationReason,
            order.IsVip,
            CanBeCancelled = order.CanBeCancelled(), // ⚡ Kullanıcıya göster
            Items = order.Items.Select(i => new
            {
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice
            })
        });
    }

    [HttpPut("{orderId:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid orderId,
        [FromBody] CancelOrderRequest? request,
        CancellationToken ct)
    {
        var command = new CancelOrderCommand(orderId, request?.Reason);
        var result = await _mediator.Send(command, ct);

        if (!result)
        {
            return BadRequest(new
            {
                error = "Order cannot be cancelled",
                message = "Order may be already cancelled, delivered, or outside the 2-hour cancellation window"
            });
        }

        return Ok(new { message = "Order cancelled successfully" });
    }
    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomerId([FromRoute] Guid customerId, CancellationToken ct)
    {
        var orders = await _repo.GetByCustomerIdAsync(customerId, ct);

        return Ok(new
        {
            customerId,
            totalOrders = orders.Count,
            orders = orders.Select(o => new
            {
                o.Id,
                o.TotalAmount,
                Status = o.Status.ToString(),
                o.CreatedAtUtc,
                o.ConfirmedAtUtc,
                o.CancelledAtUtc,
                o.CancellationReason,
                ItemCount = o.Items.Count,
                Items = o.Items.Select(i => new
                {
                    i.ProductId,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice,
                    i.TotalPrice
                })
            })
        });
    }
[HttpPost("{orderId:guid}/retry")]
public async Task<IActionResult> Retry([FromRoute] Guid orderId, CancellationToken ct)
{
    var command = new RetryOrderCommand(orderId);
    var result = await _mediator.Send(command, ct);

    if (!result)
    {
        return BadRequest(new 
        { 
            error = "Order cannot be retried",
            message = "Order must be in cancelled status and within 2 hours of creation"
        });
    }

    return Ok(new { message = "Order retry initiated successfully" });
}
}