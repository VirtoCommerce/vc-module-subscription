using VirtoCommerce.OrdersModule.Core.Model;

namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    /// <summary>
    /// Payload of <see cref="CreateSubscriptionsFromOrdersJob"/>: the newly placed orders to create subscriptions for.
    /// </summary>
    public class CreateSubscriptionsFromOrdersJobPayload
    {
        public CustomerOrder[] Orders { get; set; } = [];
    }
}
