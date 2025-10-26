namespace OrderService.Api.Controllers;


using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Contracts;
using OrderService.Application.Abstractions;
using OrderService.Application.Orders.CreateOrder;
using OrderService.Application.Orders.CancelOrder;
using OrderService.Application.Orders.RetryOrder;
using OrderService.Application.Orders.ShipOrder;
using OrderService.Application.Queue;
using OrderService.Application.Vip;

public record ShipOrderRequest(string TrackingNumber, string? Carrier = "DHL");



[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IOrderRepository _repo;
    private readonly VipOrderProcessingService _vipService;
    private readonly OrderPriorityQueue _priorityQueue;

    public OrdersController(ISender mediator, IOrderRepository repo, VipOrderProcessingService vipService, OrderPriorityQueue priorityQueue)
    {
        _mediator = mediator;
        _repo = repo;
        _vipService = vipService;
        _priorityQueue = priorityQueue;
    }


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
[HttpPut("{orderId:guid}/ship")]
public async Task<IActionResult> Ship(
    [FromRoute] Guid orderId,
    [FromBody] ShipOrderRequest request,
    CancellationToken ct)
{
    var command = new ShipOrderCommand(orderId, request.TrackingNumber, request.Carrier ?? "DHL");
    var result = await _mediator.Send(command, ct);

    if (!result)
    {
        return BadRequest(new
        {
            error = "Order cannot be shipped",
            message = "Order must be in confirmed status to be shipped"
        });
    }

    return Ok(new { message = "Order shipped successfully" });
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

    [HttpGet("vip")]
    public async Task<IActionResult> GetVipOrders(CancellationToken ct)
    {
        var vipOrders = await _vipService.GetVipOrdersAsync(ct);
        
        return Ok(new
        {
            totalVipOrders = vipOrders.Count,
            orders = vipOrders.Select(o => new
            {
                o.Id,
                o.CustomerId,
                o.TotalAmount,
                Status = o.Status.ToString(),
                o.CreatedAtUtc,
                o.ConfirmedAtUtc,
                o.CancelledAtUtc,
                o.IsVip,
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

    [HttpPost("{orderId:guid}/mark-vip")]
    public async Task<IActionResult> MarkAsVip([FromRoute] Guid orderId, CancellationToken ct)
    {
        await _vipService.MarkOrderAsVipAsync(orderId, ct);
        return Ok(new { message = "Order marked as VIP successfully" });
    }

    [HttpGet("customer/{customerId:guid}/vip-status")]
    public async Task<IActionResult> GetVipStatus([FromRoute] Guid customerId, CancellationToken ct)
    {
        var isVip = await _vipService.IsVipCustomerAsync(customerId, ct);
        return Ok(new { customerId, isVip });
    }

    [HttpGet("queue/status")]
    public IActionResult GetQueueStatus()
    {
        var status = _priorityQueue.GetStatus();
        return Ok(new
        {
            vipQueue = status.VipCount,
            regularQueue = status.RegularCount,
            totalQueue = status.TotalCount,
            message = status.VipCount > 0 ? "VIP orders being prioritized" : "Processing regular orders"
        });
    }
}