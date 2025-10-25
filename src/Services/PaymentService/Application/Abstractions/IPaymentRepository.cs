namespace PaymentService.Application.Abstractions;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken ct = default);
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken ct = default);
    Task<List<Payment>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<List<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken ct = default);
    Task<Payment> CreateAsync(Payment payment, CancellationToken ct = default);
    Task<Payment> UpdateAsync(Payment payment, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid paymentId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
