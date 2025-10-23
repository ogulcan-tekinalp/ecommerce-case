namespace OrderService.Application.Orders.RetryOrder;

using FluentValidation;

public sealed class RetryOrderCommandValidator : AbstractValidator<RetryOrderCommand>
{
    public RetryOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}