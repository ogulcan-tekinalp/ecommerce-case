using MediatR;

namespace PaymentService.Application.ProcessPayment;

public record ProcessPaymentCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    PaymentMethod Method
) : IRequest<ProcessPaymentResult>;

public record ProcessPaymentResult(
    bool Success,
    Guid? PaymentId,
    string? TransactionId,
    string? FailureReason,
    bool IsFraudulent = false,
    bool IsTimeout = false
);