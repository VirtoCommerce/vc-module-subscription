using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Services;

namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    public class CreateRecurrentOrdersJob(
        ISubscriptionBuilder builder,
        ISubscriptionSearchService subscriptionSearchService,
        ISubscriptionService subscriptionService,
        ICustomerOrderService customerOrderService)
    {
        public async Task Process()
        {
            var criteria = AbstractTypeFactory<SubscriptionSearchCriteria>.TryCreateInstance();
            criteria.Statuses = GetActiveStatuses();
            criteria.ResponseGroup = nameof(SubscriptionResponseGroup.Full);
            criteria.Take = 20;

            await foreach (var result in subscriptionSearchService.SearchBatchesAsync(criteria))
            {
                var subscriptions = result.Results;

                foreach (var subscription in subscriptions)
                {
                    await TryCreateRecurrentOrder(subscription);

                    // Prevent CustomerOrderPrototype From extra save
                    subscription.CustomerOrderPrototype = null;
                }

                await subscriptionService.SaveChangesAsync(subscriptions);
            }
        }

        protected virtual async Task TryCreateRecurrentOrder(Subscription subscription)
        {
            var subscriptionBuilder = await builder.TakeSubscription(subscription).ActualizeAsync();
            var newOrder = await subscriptionBuilder.TryToCreateRecurrentOrderAsync();
            if (newOrder != null)
            {
                await customerOrderService.SaveChangesAsync([newOrder]);
            }
        }

        protected virtual string[] GetActiveStatuses()
        {
            return
            [
                nameof(SubscriptionStatus.Active),
                nameof(SubscriptionStatus.PastDue),
                nameof(SubscriptionStatus.Trialing),
                nameof(SubscriptionStatus.Unpaid),
            ];
        }
    }
}
