namespace OrderService.Api.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Contracts;
using OrderService.Application.Abstractions;
using OrderService.Application.Orders.CreateOrder;

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
            Items: items
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
}