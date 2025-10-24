using FluentValidation;

namespace OrderService.Application.Orders.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item")
            .Must(items => items.Count <= 20).WithMessage("Order cannot contain more than 20 items");

        RuleFor(x => x.Items)
            .Must(items => items.Sum(i => i.Quantity * i.UnitPrice) >= 100)
            .WithMessage("Minimum order amount is 100 TL")
            .Must(items => items.Sum(i => i.Quantity * i.UnitPrice) <= 50000)
            .WithMessage("Maximum order amount is 50,000 TL");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductName)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Quantity cannot exceed 100 per item");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0).WithMessage("Unit price must be greater than 0");
        });
    }
}