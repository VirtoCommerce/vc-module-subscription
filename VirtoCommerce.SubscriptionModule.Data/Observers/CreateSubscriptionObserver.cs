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
    public class CreateSubscriptionObserver : IObserver<OrderChangeEvent>
    {
        private readonly ISubscriptionBuilder _subscriptionBuilder;
        public CreateSubscriptionObserver(ISubscriptionBuilder subscriptionBuilder)
        {
            _subscriptionBuilder = subscriptionBuilder;
        }

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

        public void OnNext(OrderChangeEvent value)
        {
            //Prevent creating subscription for customer orders with other operation type (it is need for preventing to handling  subscription prototype and recurring order creations)
            if (value.ChangeState == VirtoCommerce.Platform.Core.Common.EntryState.Added && !value.OrigOrder.IsPrototype && string.IsNullOrEmpty(value.OrigOrder.SubscriptionId))
            {
                var customerOrder = value.ModifiedOrder;
                try
                {
                    var subscription = _subscriptionBuilder.TryCreateSubscriptionFromOrder(value.ModifiedOrder);
                    if (subscription != null)
                    {
                        _subscriptionBuilder.TakeSubscription(subscription).Save();
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
        }
    }
}
