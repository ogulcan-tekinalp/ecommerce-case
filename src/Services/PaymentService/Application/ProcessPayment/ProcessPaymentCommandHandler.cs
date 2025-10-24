using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace PaymentService.Application.ProcessPayment;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
    private readonly PaymentProcessor _processor;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public ProcessPaymentCommandHandler(
        PaymentProcessor processor,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _processor = processor;
        _logger = logger;
        
        _retryPolicy = Policy
            .Handle<Exception>(ex => ex.Message.Contains("timeout"))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning("Payment retry {RetryCount} after {Delay}s due to: {Error}",
                        retryCount, timeSpan.TotalSeconds, exception.Message);
                });
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

        PaymentResult result;

        try
        {
            result = await _retryPolicy.ExecuteAsync(async () =>
            {
                payment.RetryCount++;
                var attemptResult = await _processor.ProcessPaymentAsync(payment, ct);
                
                if (attemptResult.IsTimeout && payment.RetryCount < 3)
                {
                    throw new Exception("Payment gateway timeout");
                }
                
                return attemptResult;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment failed after retries for Order {OrderId}", request.OrderId);
            return new ProcessPaymentResult(
                Success: false,
                PaymentId: payment.Id,
                TransactionId: null,
                FailureReason: "Payment failed after maximum retries",
                IsFraudulent: false,
                IsTimeout: true
            );
        }

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