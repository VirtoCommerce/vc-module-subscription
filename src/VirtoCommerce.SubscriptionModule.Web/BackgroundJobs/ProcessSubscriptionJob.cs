using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Services;

namespace VirtoCommerce.SubscriptionModule.Web.BackgroundJobs
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

        [DisableConcurrentExecution(10)]
        // "DisableConcurrentExecutionAttribute" prevents to start simultaneous job payloads.
	// Should have short timeout, because this attribute implemented by following manner: newly started job falls into "processing" state immediately.
        // Then it tries to receive job lock during timeout. If the lock received, the job starts payload.
        // When the job is awaiting desired timeout for lock release, it stucks in "processing" anyway. (Therefore, you should not to set long timeouts (like 24*60*60), this will cause a lot of stucked jobs and performance degradation.)
        // Then, if timeout is over and the lock NOT acquired, the job falls into "scheduled" state (this is default fail-retry scenario).
	// Failed job goes to "Failed" state (by default) after retries exhausted.
        public async Task Process()
        {
            var criteria = new SubscriptionSearchCriteria
            {
                Statuses = new[] { SubscriptionStatus.Active, SubscriptionStatus.PastDue, SubscriptionStatus.Trialing, SubscriptionStatus.Unpaid }.Select(x => x.ToString()).ToArray(),
                Take = 0
            };
            var result = await _subscriptionSearchService.SearchSubscriptionsAsync(criteria);
            var batchSize = 20;
            for (var i = 0; i < result.TotalCount; i += batchSize)
            {
                criteria.Skip = i;
                criteria.Take = batchSize;
                result = await _subscriptionSearchService.SearchSubscriptionsAsync(criteria);
                var subscriptions = await _subscriptionService.GetByIdsAsync(result.Results.Select(x => x.Id).ToArray());
                foreach (var subscription in subscriptions)
                {
                    await _subscriptionBuilder.TakeSubscription(subscription).ActualizeAsync();
                }
                await _subscriptionService.SaveSubscriptionsAsync(subscriptions);
            }
        }

    }
}
