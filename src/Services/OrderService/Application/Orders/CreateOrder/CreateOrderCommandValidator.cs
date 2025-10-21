using FluentValidation;

namespace OrderService.Application.Orders.CreateOrder;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(100).LessThanOrEqualTo(50000);
    }
}
