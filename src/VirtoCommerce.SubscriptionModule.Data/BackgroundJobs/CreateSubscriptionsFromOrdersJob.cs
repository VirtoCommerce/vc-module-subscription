using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.SubscriptionModule.Data.Handlers;

namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    /// <summary>
    /// Creates subscriptions for newly placed orders, off the request thread. The logic stays on
    /// <see cref="CreateSubscriptionOrderChangedEventHandler.HandleOrderChangesInBackground"/>, which existing
    /// customizations may override.
    /// </summary>
    public class CreateSubscriptionsFromOrdersJob(CreateSubscriptionOrderChangedEventHandler orderChangedEventHandler)
        : IBackgroundJobHandler<CreateSubscriptionsFromOrdersJobPayload>
    {
        public virtual Task Execute(CreateSubscriptionsFromOrdersJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            orderChangedEventHandler.HandleOrderChangesInBackground(payload.Orders);
            return Task.CompletedTask;
        }
    }
}
