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
    public class CreateSubscriptionOrderChangedEventHandler(
        ISubscriptionBuilder subscriptionBuilder,
        ISubscriptionService subscriptionService,
        ICustomerOrderService customerOrderService)
        : IEventHandler<OrderChangedEvent>
    {
        public virtual Task Handle(OrderChangedEvent message)
        {
            var addedOrders = message.ChangedEntries.Where(x => x.EntryState == EntryState.Added).Select(e => e.NewEntry).ToArray();
            BackgroundJob.Enqueue(() => HandleOrderChangesInBackground(addedOrders));

            return Task.CompletedTask;
        }

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
                    var subscription = await subscriptionBuilder.TryCreateSubscriptionFromOrderAsync(customerOrder);
                    if (subscription != null)
                    {
                        // Actualize subscription to ensure it has the correct status and balance
                        await subscriptionBuilder.TakeSubscription(subscription).ActualizeAsync();

                        // Save Subscription changes
                        await subscriptionService.SaveChangesAsync([subscription]);

                        //Link subscription with customer order
                        customerOrder.SubscriptionId = subscription.Id;
                        customerOrder.SubscriptionNumber = subscription.Number;
                        //Save order changes
                        await customerOrderService.SaveChangesAsync([customerOrder]);
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
