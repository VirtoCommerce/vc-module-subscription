using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Services;

namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    public class CreateRecurrentOrdersJob
    {

        private readonly ISubscriptionBuilder _subscriptionBuilder;
        private readonly ISubscriptionSearchService _subscriptionSearchService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ICustomerOrderService _customerOrderService;

        public CreateRecurrentOrdersJob(ISubscriptionBuilder subscriptionBuilder, ISubscriptionSearchService subscriptionSearchService,
            ISubscriptionService subscriptionService, ICustomerOrderService customerOrderService)
        {
            _subscriptionBuilder = subscriptionBuilder;
            _subscriptionSearchService = subscriptionSearchService;
            _subscriptionService = subscriptionService;
            _customerOrderService = customerOrderService;
        }

        public async Task Process()
        {
            var criteria = new SubscriptionSearchCriteria
            {
                Statuses = GetActiveStatuses(),
                Take = 0,
            };

            var result = await _subscriptionSearchService.SearchAsync(criteria);
            var batchSize = 20;

            for (var i = 0; i < result.TotalCount; i += batchSize)
            {
                criteria.Skip = i;
                criteria.Take = batchSize;
                criteria.ResponseGroup = (SubscriptionResponseGroup.Full).ToString();

                result = await _subscriptionSearchService.SearchAsync(criteria);
                var subscriptions = result.Results;

                foreach (var subscription in subscriptions)
                {
                    await TryCreateRecurrentOrder(subscription);
                    // Prevent CustomerOrderPrototype From extra save
                    subscription.CustomerOrderPrototype = null;
                }

                await _subscriptionService.SaveChangesAsync(subscriptions);
            }
        }

        protected virtual async Task TryCreateRecurrentOrder(Subscription subscription)
        {
            var subscriptionBuilder = await _subscriptionBuilder.TakeSubscription(subscription).ActualizeAsync();
            var newOrder = await subscriptionBuilder.TryToCreateRecurrentOrderAsync();
            if (newOrder != null)
            {
                await _customerOrderService.SaveChangesAsync([newOrder]);
            }
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
