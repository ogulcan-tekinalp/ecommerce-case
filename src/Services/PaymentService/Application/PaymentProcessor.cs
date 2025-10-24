using Microsoft.Extensions.Logging;

namespace PaymentService.Application;

public class PaymentProcessor
{
    private readonly ILogger<PaymentProcessor> _logger;
    private readonly Random _random = new();

    public PaymentProcessor(ILogger<PaymentProcessor> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(Payment payment, CancellationToken ct = default)
    {
        _logger.LogInformation("💳 Processing payment {PaymentId} for Order {OrderId}, Amount: {Amount}",
            payment.Id, payment.OrderId, payment.Amount);

        if (IsFraudulent(payment))
        {
            _logger.LogWarning("🚨 Fraud detected for payment {PaymentId}", payment.Id);
            return new PaymentResult
            {
                Success = false,
                TransactionId = null,
                FailureReason = "Fraud detection triggered",
                IsFraudulent = true
            };
        }

        await Task.Delay(TimeSpan.FromSeconds(1), ct);

        var outcome = _random.Next(100);

        if (outcome < 85)
        {
            var transactionId = Guid.NewGuid().ToString("N")[..16].ToUpper();
            _logger.LogInformation("✅ Payment {PaymentId} successful, Transaction: {TransactionId}",
                payment.Id, transactionId);

            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                FailureReason = null,
                IsFraudulent = false
            };
        }
        else if (outcome < 95)
        {
            _logger.LogWarning("⏱️ Payment {PaymentId} timeout", payment.Id);
            return new PaymentResult
            {
                Success = false,
                TransactionId = null,
                FailureReason = "Payment gateway timeout",
                IsTimeout = true
            };
        }
        else
        {
            _logger.LogWarning("❌ Payment {PaymentId} failed", payment.Id);
            return new PaymentResult
            {
                Success = false,
                TransactionId = null,
                FailureReason = "Payment declined by bank"
            };
        }
    }

    private bool IsFraudulent(Payment payment)
    {
        if (payment.Amount > 100000)
        {
            return true;
        }

        if (payment.RetryCount > 3)
        {
            return true;
        }

        return false;
    }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? FailureReason { get; set; }
    public bool IsFraudulent { get; set; }
    public bool IsTimeout { get; set; }
}