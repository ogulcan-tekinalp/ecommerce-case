using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application;
using PaymentService.Application.ProcessPayment;

namespace PaymentService.Api.Controllers;
[ApiController]
[Route("api/v1/payments")]
public class PaymentController : ControllerBase
{
    private readonly ISender _mediator;

    public PaymentController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequest request,
        CancellationToken ct)
    {
        var command = new ProcessPaymentCommand(
            request.OrderId,
            request.CustomerId,
            request.Amount,
            request.Method
        );

        var result = await _mediator.Send(command, ct);

        if (!result.Success)
        {
            return BadRequest(new
            {
                error = result.FailureReason,
                isFraudulent = result.IsFraudulent,
                isTimeout = result.IsTimeout
            });
        }

        return Ok(new
        {
            paymentId = result.PaymentId,
            transactionId = result.TransactionId,
            message = "Payment processed successfully"
        });
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidatePaymentMethod(
        [FromBody] ValidatePaymentRequest request)
    {
        // Simple validation mock
        var isValid = request.Method >= 0 && (int)request.Method <= 3;
        
        return Ok(new
        {
            isValid,
            method = request.Method.ToString()
        });
    }
}

public record ProcessPaymentRequest(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    PaymentMethod Method
);

public record ValidatePaymentRequest(PaymentMethod Method);