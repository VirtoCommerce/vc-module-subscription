using FluentValidation;
using VirtoCommerce.SubscriptionModule.Core.Model;

namespace VirtoCommerce.SubscriptionModule.Data.Validation
{
    public class SubscriptionValidator : AbstractValidator<Subscription>
    {
        public SubscriptionValidator()
        {
            RuleFor(subscription => subscription.StoreId).NotNull().NotEmpty();
            RuleFor(subscription => subscription.CustomerId).NotNull().NotEmpty();
        }
    }
}
