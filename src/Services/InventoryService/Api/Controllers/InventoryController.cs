namespace InventoryService.Api.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using InventoryService.Application.Inventory.ReserveStock;
using InventoryService.Application.Inventory.ReleaseStock;
using InventoryService.Application.Inventory.CheckAvailability;
using InventoryService.Application.Inventory.GetProductStock;

[ApiController]
[Route("api/v1/inventory")]
public class InventoryController : ControllerBase
{
    private readonly ISender _mediator;

    public InventoryController(ISender mediator) => _mediator = mediator;

    [HttpPost("check-availability")]
    public async Task<IActionResult> CheckAvailability(
        [FromBody] CheckAvailabilityRequest request,
        CancellationToken ct)
    {
        var query = new CheckAvailabilityQuery(
            request.Items.Select(i => new CheckAvailabilityItemDto(i.ProductId, i.Quantity)).ToList()
        );

        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }
    [HttpGet("products/{productId}/stock")]
public async Task<IActionResult> GetProductStock(Guid productId, CancellationToken ct)
{
    var query = new GetProductStockQuery(productId);
    var result = await _mediator.Send(query, ct);

    if (result == null)
        return NotFound(new { error = "Product not found" });

    return Ok(result);
}

    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve(
        [FromBody] ReserveStockRequest request,
        CancellationToken ct)
    {
        var command = new ReserveStockCommand(
            request.OrderId,
            request.Items.Select(i => new ReserveStockItemDto(i.ProductId, i.Quantity)).ToList()
        );

        var result = await _mediator.Send(command, ct);

        if (!result.Success)
        {
            return BadRequest(new { error = result.FailureReason });
        }

        return Ok(new { reservationId = result.ReservationId });
    }

    [HttpPost("release")]
    public async Task<IActionResult> Release(
        [FromBody] ReleaseStockRequest request,
        CancellationToken ct)
    {
        var command = new ReleaseStockCommand(request.ReservationId, request.Reason ?? "Manual release");
        var result = await _mediator.Send(command, ct);

        if (!result)
        {
            return BadRequest(new { error = "Failed to release reservation" });
        }

        return Ok(new { message = "Stock released successfully" });
    }
}