using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Services;

namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    public class ProcessSubscriptionJob
    {
        private readonly ISubscriptionBuilder _subscriptionBuilder;
        private readonly ISubscriptionSearchService _subscriptionSearchService;
        private readonly ISubscriptionService _subscriptionService;
        public ProcessSubscriptionJob(ISubscriptionBuilder subscriptionBuilder, ISubscriptionSearchService subscriptionSearchService,
                                      ISubscriptionService subscriptionService)
        {
            _subscriptionBuilder = subscriptionBuilder;
            _subscriptionSearchService = subscriptionSearchService;
            _subscriptionService = subscriptionService;
        }

        public async Task Process()
        {
            var criteria = new SubscriptionSearchCriteria
            {
                Statuses = GetActiveStatuses(),
                Take = 0
            };

            var result = await _subscriptionSearchService.SearchAsync(criteria);
            var batchSize = 20;

            for (var i = 0; i < result.TotalCount; i += batchSize)
            {
                criteria.Skip = i;
                criteria.Take = batchSize;
                criteria.ResponseGroup = (SubscriptionResponseGroup.Default | SubscriptionResponseGroup.WithRelatedOrders).ToString();

                result = await _subscriptionSearchService.SearchAsync(criteria);

                var subscriptions = result.Results;
                foreach (var subscription in subscriptions)
                {
                    await ActualizeSubscriptionStatus(subscription);
                }

                await _subscriptionService.SaveChangesAsync(subscriptions);
            }
        }

        protected virtual Task ActualizeSubscriptionStatus(Subscription subscription)
        {
            return _subscriptionBuilder.TakeSubscription(subscription).ActualizeAsync();
        }

        protected virtual string[] GetActiveStatuses()
        {
            return new[] {
                SubscriptionStatus.Active,
                SubscriptionStatus.PastDue,
                SubscriptionStatus.Trialing,
                SubscriptionStatus.Unpaid
            }.Select(x => x.ToString()).ToArray();
        }
    }
}
