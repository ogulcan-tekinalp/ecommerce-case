using FluentValidation;
using OrderService.Application.Orders.CreateOrder;

namespace OrderService.Application.Validators;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("En az bir kalem girmelisiniz.")
            // Total quantity across all items cannot exceed 20
            .Must(items => items.Sum(i => i.Quantity) <= 20)
            .WithMessage("Toplam adet 20'yi geçemez.");

        // Also limit number of distinct line items to 20
        RuleFor(x => x.Items.Count)
            .LessThanOrEqualTo(20)
            .WithMessage("Bir siparişte en fazla 20 farklı ürün olabilir.");

        // Business rules: total amount between 100 and 50_000
        RuleFor(x => x.Items)
            .Must(items =>
            {
                var total = items.Sum(i => i.Quantity * i.UnitPrice);
                return total >= 100m && total <= 50000m;
            })
            .WithMessage("Sipariş tutarı 100 TL ile 50.000 TL arasında olmalıdır.");
    }
}
