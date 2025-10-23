using FluentValidation;

namespace OrderService.Application.Orders.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Items null olamaz.")
            .NotEmpty().WithMessage("En az bir kalem girmelisiniz.");

        RuleFor(x => x.Items.Sum(i => i.Quantity))
            .LessThanOrEqualTo(20)
            .WithMessage("Toplam adet 20'yi geçemez.");

        RuleForEach(x => x.Items).ChildRules(items =>
        {
            items.RuleFor(i => i.ProductId).NotEmpty().WithMessage("ProductId zorunludur.");
            items.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity > 0 olmalı.");
        });
    }
}
