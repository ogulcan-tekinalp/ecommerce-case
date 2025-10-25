using Microsoft.Extensions.Logging;
using PaymentService.Application.Abstractions;

namespace PaymentService.Application.FraudDetection;

public class FraudDetectionService : IFraudDetectionService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<FraudDetectionService> _logger;

    public FraudDetectionService(
        IPaymentRepository paymentRepository,
        ILogger<FraudDetectionService> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<FraudDetectionResult> AnalyzePaymentAsync(Payment payment, CancellationToken ct = default)
    {
        var result = new FraudDetectionResult();
        var triggers = new List<string>();

        _logger.LogInformation("🔍 Analyzing payment {PaymentId} for fraud", payment.Id);

        // Rule 1: High amount threshold
        if (payment.Amount > 100000)
        {
            result.RiskScore += 30;
            triggers.Add("High amount threshold exceeded");
        }

        // Rule 2: Multiple retry attempts
        if (payment.RetryCount > 3)
        {
            result.RiskScore += 25;
            triggers.Add("Excessive retry attempts");
        }

        // Rule 3: Check for recent failed payments by same customer
        var recentFailedPayments = await _paymentRepository.GetByCustomerIdAsync(payment.CustomerId, ct);
        var failedInLastHour = recentFailedPayments
            .Where(p => p.Status == PaymentStatus.Failed && 
                       p.CreatedAtUtc > DateTime.UtcNow.AddHours(-1))
            .Count();

        if (failedInLastHour >= 3)
        {
            result.RiskScore += 20;
            triggers.Add("Multiple failed payments in last hour");
        }

        // Rule 4: Check for rapid successive payments
        var recentPayments = recentFailedPayments
            .Where(p => p.CreatedAtUtc > DateTime.UtcNow.AddMinutes(-10))
            .Count();

        if (recentPayments >= 5)
        {
            result.RiskScore += 15;
            triggers.Add("Rapid successive payments");
        }

        // Rule 5: Unusual payment method patterns
        if (payment.Method == PaymentMethod.Wallet && payment.Amount > 50000)
        {
            result.RiskScore += 10;
            triggers.Add("High amount wallet payment");
        }

        // Rule 6: Time-based analysis (late night payments)
        var hour = DateTime.UtcNow.Hour;
        if (hour >= 2 && hour <= 5 && payment.Amount > 20000)
        {
            result.RiskScore += 15;
            triggers.Add("Unusual time high amount payment");
        }

        // Rule 7: Round number amounts (potential test payments)
        if (payment.Amount % 1000 == 0 && payment.Amount > 10000)
        {
            result.RiskScore += 5;
            triggers.Add("Round number high amount");
        }

        result.Triggers = triggers;
        result.IsFraudulent = result.RiskScore >= 30;
        result.Reason = result.IsFraudulent 
            ? $"Fraud detected: {string.Join(", ", triggers)}" 
            : "Payment appears legitimate";

        _logger.LogInformation("🔍 Fraud analysis for Payment {PaymentId}: RiskScore={RiskScore}, IsFraudulent={IsFraudulent}, Triggers={Triggers}",
            payment.Id, result.RiskScore, result.IsFraudulent, string.Join(", ", triggers));

        return result;
    }
}
