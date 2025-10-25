using MediatR;

namespace PaymentService.Application.RefundPayment;

public record RefundPaymentCommand(
    Guid PaymentId,
    decimal Amount,
    string Reason
) : IRequest<RefundPaymentResult>;

public record RefundPaymentResult(
    bool Success,
    Guid? RefundId,
    string? TransactionId,
    string? FailureReason
);
