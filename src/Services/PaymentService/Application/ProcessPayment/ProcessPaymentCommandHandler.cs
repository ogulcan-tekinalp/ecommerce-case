using MediatR;
using Microsoft.Extensions.Logging;

namespace PaymentService.Application.ProcessPayment;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
    private readonly PaymentProcessor _processor;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        PaymentProcessor processor,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public async Task<ProcessPaymentResult> Handle(ProcessPaymentCommand request, CancellationToken ct)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Method = request.Method,
            Status = PaymentStatus.Processing,
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        };

        var result = await _processor.ProcessPaymentAsync(payment, ct);

        return new ProcessPaymentResult(
            Success: result.Success,
            PaymentId: payment.Id,
            TransactionId: result.TransactionId,
            FailureReason: result.FailureReason,
            IsFraudulent: result.IsFraudulent,
            IsTimeout: result.IsTimeout
        );
    }
}