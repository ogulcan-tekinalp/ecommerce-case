namespace PaymentService.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentService.Application;
using PaymentService.Application.Abstractions;
using PaymentService.Application.RefundPayment;
using Xunit;

public class RefundPaymentCommandHandlerTests
{
    private readonly Mock<IPaymentRepository> _mockRepository;
    private readonly Mock<ILogger<RefundPaymentCommandHandler>> _mockLogger;
    private readonly RefundPaymentCommandHandler _handler;

    public RefundPaymentCommandHandlerTests()
    {
        _mockRepository = new Mock<IPaymentRepository>();
        _mockLogger = new Mock<ILogger<RefundPaymentCommandHandler>>();
        _handler = new RefundPaymentCommandHandler(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_PaymentNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new RefundPaymentCommand(Guid.NewGuid(), 1000m, "Test refund");
        _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("Payment not found");
    }

    [Fact]
    public async Task Handle_PaymentNotSuccessful_ShouldReturnFailure()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Failed,
            Amount = 1000m
        };

        var command = new RefundPaymentCommand(payment.Id, 500m, "Test refund");
        _mockRepository.Setup(x => x.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("Cannot refund payment with status Failed");
    }

    [Fact]
    public async Task Handle_RefundAmountExceedsPayment_ShouldReturnFailure()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Success,
            Amount = 1000m
        };

        var command = new RefundPaymentCommand(payment.Id, 1500m, "Test refund"); // More than payment amount
        _mockRepository.Setup(x => x.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("Refund amount cannot exceed original payment amount");
    }

    [Fact]
    public async Task Handle_ValidRefund_ShouldReturnSuccess()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Success,
            Amount = 1000m
        };

        var command = new RefundPaymentCommand(payment.Id, 500m, "Test refund");
        _mockRepository.Setup(x => x.GetByIdAsync(payment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.RefundId.Should().NotBeNull();
        result.TransactionId.Should().NotBeNullOrEmpty();
        result.TransactionId.Should().StartWith("REF-");
    }
}
