using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using VirtoCommerce.OrdersModule.Core.Events;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Data.Exceptions;

namespace VirtoCommerce.SubscriptionModule.Data.Handlers
{
    public class CreateSubscriptionOrderChangedEventHandler : IEventHandler<OrderChangedEvent>
    {
        private readonly ISubscriptionBuilder _subscriptionBuilder;
        private readonly ISubscriptionService _subscriptionService;
        private readonly ICustomerOrderService _customerOrderService;
        public CreateSubscriptionOrderChangedEventHandler(ISubscriptionBuilder subscriptionBuilder, ISubscriptionService subscriptionService, ICustomerOrderService customerOrderService)
        {
            _subscriptionBuilder = subscriptionBuilder;
            _subscriptionService = subscriptionService;
            _customerOrderService = customerOrderService;
        }

        public virtual Task Handle(OrderChangedEvent message)
        {
            var addedOrders = message.ChangedEntries.Where(x => x.EntryState == EntryState.Added).Select(e => e.NewEntry).ToArray();
            BackgroundJob.Enqueue(() => HandleOrderChangesInBackground(addedOrders));

            return Task.CompletedTask;
        }

        [DisableConcurrentExecution(10)]
        // "DisableConcurrentExecutionAttribute" prevents to start simultaneous job payloads.
	// Should have short timeout, because this attribute implemented by following manner: newly started job falls into "processing" state immediately.
        // Then it tries to receive job lock during timeout. If the lock received, the job starts payload.
        // When the job is awaiting desired timeout for lock release, it stucks in "processing" anyway. (Therefore, you should not to set long timeouts (like 24*60*60), this will cause a lot of stucked jobs and performance degradation.)
        // Then, if timeout is over and the lock NOT acquired, the job falls into "scheduled" state (this is default fail-retry scenario).
	// Failed job goes to "Failed" state (by default) after retries exhausted.
        public virtual void HandleOrderChangesInBackground(CustomerOrder[] orders)
        {
            foreach (var order in orders)
            {
                HandleOrderChanges(order);
            }
        }

        public void HandleOrderChanges(CustomerOrder order)
        {
            HandleOrderChangesAsync(order).GetAwaiter().GetResult();
        }

        protected virtual async Task HandleOrderChangesAsync(CustomerOrder customerOrder)
        {
            //Prevent creating subscription for customer orders with other operation type (it is need for preventing to handling  subscription prototype and recurring order creations)
            if (!customerOrder.IsPrototype && string.IsNullOrEmpty(customerOrder.SubscriptionId))
            {
                try
                {
                    var subscription = await _subscriptionBuilder.TryCreateSubscriptionFromOrderAsync(customerOrder);
                    if (subscription != null)
                    {
                        await _subscriptionBuilder.TakeSubscription(subscription).ActualizeAsync();
                        await _subscriptionService.SaveSubscriptionsAsync(new[] { subscription });
                        //Link subscription with customer order
                        customerOrder.SubscriptionId = subscription.Id;
                        customerOrder.SubscriptionNumber = subscription.Number;
                        //Save order changes
                        await _customerOrderService.SaveChangesAsync(new[] { customerOrder });
                    }
                }
                catch (Exception ex)
                {
                    throw new CreateSubscriptionException(ex);
                }
            }
        }
    }
}
