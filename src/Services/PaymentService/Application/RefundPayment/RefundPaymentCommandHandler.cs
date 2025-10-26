using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Abstractions;

namespace PaymentService.Application.RefundPayment;

public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, RefundPaymentResult>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<RefundPaymentCommandHandler> _logger;

    public RefundPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        ILogger<RefundPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _logger = logger;
    }

    public async Task<RefundPaymentResult> Handle(RefundPaymentCommand request, CancellationToken ct)
    {
        _logger.LogInformation("🔄 Processing refund for Payment {PaymentId}, Amount: {Amount}",
            request.PaymentId, request.Amount);

        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, ct);
        if (payment == null)
        {
            _logger.LogWarning("Payment {PaymentId} not found for refund", request.PaymentId);
            return new RefundPaymentResult(
                Success: false,
                RefundId: null,
                TransactionId: null,
                FailureReason: "Payment not found"
            );
        }

        // Business rules for refund
        if (payment.Status != PaymentStatus.Success)
        {
            _logger.LogWarning("Cannot refund payment {PaymentId} with status {Status}",
                request.PaymentId, payment.Status);
            return new RefundPaymentResult(
                Success: false,
                RefundId: null,
                TransactionId: null,
                FailureReason: $"Cannot refund payment with status {payment.Status}"
            );
        }

        if (request.Amount > payment.Amount)
        {
            _logger.LogWarning("Refund amount {RefundAmount} exceeds payment amount {PaymentAmount}",
                request.Amount, payment.Amount);
            return new RefundPaymentResult(
                Success: false,
                RefundId: null,
                TransactionId: null,
                FailureReason: "Refund amount cannot exceed original payment amount"
            );
        }

        // Check if already refunded
        if (payment.Status == PaymentStatus.Refunded)
        {
            _logger.LogWarning("Payment {PaymentId} already refunded", request.PaymentId);
            return new RefundPaymentResult(
                Success: false,
                RefundId: null,
                TransactionId: null,
                FailureReason: "Payment already refunded"
            );
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);

            // Update payment status
            payment.Status = PaymentStatus.Refunded;
            payment.RefundedAtUtc = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment, ct);

            var refundId = Guid.NewGuid();
            var refundTransactionId = $"REF-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";

            _logger.LogInformation("✅ Refund successful for Payment {PaymentId}, RefundId: {RefundId}, Transaction: {TransactionId}",
                request.PaymentId, refundId, refundTransactionId);

            return new RefundPaymentResult(
                Success: true,
                RefundId: refundId,
                TransactionId: refundTransactionId,
                FailureReason: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process refund for Payment {PaymentId}", request.PaymentId);
            return new RefundPaymentResult(
                Success: false,
                RefundId: null,
                TransactionId: null,
                FailureReason: "Internal error during refund processing"
            );
        }
    }
}
