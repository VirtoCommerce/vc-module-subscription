using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.SubscriptionModule.Data.Handlers;

namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    /// <summary>
    /// Creates subscriptions for newly placed orders, off the request thread. Loads the orders by id, then leaves the
    /// logic to <see cref="CreateSubscriptionOrderChangedEventHandler.HandleOrderChangesInBackground"/>, which existing
    /// customizations may override.
    /// </summary>
    public class CreateSubscriptionsFromOrdersJob(
        CreateSubscriptionOrderChangedEventHandler orderChangedEventHandler,
        ICustomerOrderService customerOrderService)
        : IBackgroundJobHandler<CreateSubscriptionsFromOrdersJobPayload>
    {
        public virtual async Task Execute(CreateSubscriptionsFromOrdersJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            if (payload.OrderIds.IsNullOrEmpty())
            {
                return;
            }

            var orders = await customerOrderService.GetAsync(payload.OrderIds);
            orderChangedEventHandler.HandleOrderChangesInBackground(orders.ToArray());
        }
    }
}
