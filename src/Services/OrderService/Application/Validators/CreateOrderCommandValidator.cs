using FluentValidation;
using OrderService.Application.Orders.CreateOrder;

namespace OrderService.Application.Validators;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("En az bir kalem girmelisiniz.")
            .Must(items => items.Sum(i => i.Quantity) <= 20)
            .WithMessage("Toplam adet 20'yi geçemez.");
    }
}
