using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Data.Exceptions;
using VirtoCommerce.Domain.Order.Events;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;

namespace VirtoCommerce.SubscriptionModule.Data.Observers
{
    /// <summary>
    /// Create new subscription for new recurrent order
    /// </summary>
    public class OrderSubscriptionObserver : IObserver<OrderChangeEvent>
    {
        private readonly ISubscriptionBuilder _subscriptionBuilder;
        private readonly ISubscriptionService _subscriptionService;
        public OrderSubscriptionObserver(ISubscriptionBuilder subscriptionBuilder, ISubscriptionService subscriptionService)
        {
            _subscriptionBuilder = subscriptionBuilder;
            _subscriptionService = subscriptionService;
        }

        #region IObserver<OrderChangeEvent> Members
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            //Throw exception because errors in this observer cannot be swallowed
            var createSubscriptionException = error as CreateSubscriptionException;
            if (createSubscriptionException != null)
            {
                throw createSubscriptionException;
            }
        }

        public void OnNext(OrderChangeEvent orderChangeEvent)
        {
            var customerOrder = orderChangeEvent.ModifiedOrder;
    
            //Prevent creating subscription for customer orders with other operation type (it is need for preventing to handling  subscription prototype and recurring order creations)
            if (orderChangeEvent.ChangeState == VirtoCommerce.Platform.Core.Common.EntryState.Added && !customerOrder.IsPrototype && string.IsNullOrEmpty(customerOrder.SubscriptionId))
            {
                try
                {
                    var subscription = _subscriptionBuilder.TryCreateSubscriptionFromOrder(orderChangeEvent.ModifiedOrder);
                    if (subscription != null)
                    {
                        _subscriptionBuilder.TakeSubscription(subscription).Actualize();
                        _subscriptionService.SaveSubscriptions(new[] { subscription });
                        //Link subscription with customer order
                        customerOrder.SubscriptionId = subscription.Id;
                        customerOrder.SubscriptionNumber = subscription.Number;
                    }
                }
                catch (Exception ex)
                {
                    throw new CreateSubscriptionException(ex);
                }
            }
            else if (!string.IsNullOrEmpty(customerOrder.SubscriptionId))
            {
                var subscription = _subscriptionService.GetByIds(new[] { customerOrder.SubscriptionId }).FirstOrDefault();
                if (subscription != null)
                {
                    //Replace original order in subscription to modified 
                    var origOrder = subscription.CustomerOrders.FirstOrDefault(x => x.Id == customerOrder.Id);
                    if (origOrder != null)
                    {
                        subscription.CustomerOrders.Remove(origOrder);
                    }
                    subscription.CustomerOrders.Add(customerOrder);

                    _subscriptionBuilder.TakeSubscription(subscription).Actualize();
                    _subscriptionService.SaveSubscriptions(new[] { subscription });
                }
            }
        } 
        #endregion
    }
}
