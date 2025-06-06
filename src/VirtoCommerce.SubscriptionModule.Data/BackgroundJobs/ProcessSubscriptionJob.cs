using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Services;

namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    public class ProcessSubscriptionJob(
        ISubscriptionBuilder subscriptionBuilder,
        ISubscriptionSearchService subscriptionSearchService,
        ISubscriptionService subscriptionService)
    {
        public async Task Process()
        {
            var criteria = AbstractTypeFactory<SubscriptionSearchCriteria>.TryCreateInstance();
            criteria.Statuses = GetActiveStatuses();
            criteria.ResponseGroup = (SubscriptionResponseGroup.Default | SubscriptionResponseGroup.WithRelatedOrders).ToString();
            criteria.Take = 20;

            await foreach (var result in subscriptionSearchService.SearchBatchesAsync(criteria))
            {
                var subscriptions = result.Results;

                foreach (var subscription in subscriptions)
                {
                    await ActualizeSubscriptionStatus(subscription);
                }

                await subscriptionService.SaveChangesAsync(subscriptions);
            }
        }

        protected virtual Task ActualizeSubscriptionStatus(Subscription subscription)
        {
            return subscriptionBuilder.TakeSubscription(subscription).ActualizeAsync();
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
