using PaymentService.Application;

namespace PaymentService.Application.FraudDetection;

public interface IFraudDetectionService
{
    Task<FraudDetectionResult> AnalyzePaymentAsync(Payment payment, CancellationToken ct = default);
}

public class FraudDetectionResult
{
    public bool IsFraudulent { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal RiskScore { get; set; }
    public List<string> Triggers { get; set; } = new();
}
