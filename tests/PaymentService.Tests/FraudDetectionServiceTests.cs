namespace PaymentService.Tests;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentService.Application;
using PaymentService.Application.Abstractions;
using PaymentService.Application.FraudDetection;
using Xunit;

public class FraudDetectionServiceTests
{
    private readonly Mock<IPaymentRepository> _mockRepository;
    private readonly Mock<ILogger<FraudDetectionService>> _mockLogger;
    private readonly FraudDetectionService _fraudDetectionService;

    public FraudDetectionServiceTests()
    {
        _mockRepository = new Mock<IPaymentRepository>();
        _mockLogger = new Mock<ILogger<FraudDetectionService>>();
        _fraudDetectionService = new FraudDetectionService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task AnalyzePaymentAsync_HighAmount_ShouldDetectFraud()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 150000, // Above 100,000 threshold
            CustomerId = Guid.NewGuid(),
            RetryCount = 0
        };

        _mockRepository.Setup(x => x.GetByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        // Act
        var result = await _fraudDetectionService.AnalyzePaymentAsync(payment);

        // Assert
        result.IsFraudulent.Should().BeTrue();
        result.RiskScore.Should().BeGreaterOrEqualTo(30);
        result.Triggers.Should().Contain("High amount threshold exceeded");
    }

    [Fact]
    public async Task AnalyzePaymentAsync_MultipleRetries_ShouldDetectFraud()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 50000,
            CustomerId = Guid.NewGuid(),
            RetryCount = 5 // Above 3 threshold
        };

        _mockRepository.Setup(x => x.GetByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        // Act
        var result = await _fraudDetectionService.AnalyzePaymentAsync(payment);

        // Assert
        result.IsFraudulent.Should().BeTrue();
        result.RiskScore.Should().BeGreaterOrEqualTo(25);
        result.Triggers.Should().Contain("Excessive retry attempts");
    }

    [Fact]
    public async Task AnalyzePaymentAsync_MultipleFailedPayments_ShouldDetectFraud()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 120000, // High amount to get 30 points
            CustomerId = Guid.NewGuid(),
            RetryCount = 0
        };

        var recentFailedPayments = new List<Payment>
        {
            new() { Status = PaymentStatus.Failed, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30) },
            new() { Status = PaymentStatus.Failed, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-20) },
            new() { Status = PaymentStatus.Failed, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10) },
            new() { Status = PaymentStatus.Failed, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5) }
        };

        _mockRepository.Setup(x => x.GetByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentFailedPayments);

        // Act
        var result = await _fraudDetectionService.AnalyzePaymentAsync(payment);

        // Assert
        result.IsFraudulent.Should().BeTrue();
        result.RiskScore.Should().BeGreaterOrEqualTo(20);
        result.Triggers.Should().Contain("Multiple failed payments in last hour");
    }

    [Fact]
    public async Task AnalyzePaymentAsync_LegitimatePayment_ShouldNotDetectFraud()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 5000, // Normal amount
            CustomerId = Guid.NewGuid(),
            RetryCount = 0
        };

        _mockRepository.Setup(x => x.GetByCustomerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Payment>());

        // Act
        var result = await _fraudDetectionService.AnalyzePaymentAsync(payment);

        // Assert
        result.IsFraudulent.Should().BeFalse();
        result.RiskScore.Should().BeLessThan(50);
        result.Reason.Should().Be("Payment appears legitimate");
    }
}
