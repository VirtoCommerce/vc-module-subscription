using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubscriptionModule.Core.Services;
using SubscriptionModule.Data.Exceptions;
using VirtoCommerce.Domain.Order.Events;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;

namespace SubscriptionModule.Data.Observers
{
    public class CreateSubscriptionObserver : IObserver<OrderChangedEvent>
    {
        private readonly ISubscriptionBuilder _subscriptionBuilder;
        private readonly ISubscriptionService _subscriptionService;
        public CreateSubscriptionObserver(ISubscriptionBuilder subscriptionBuilder, ISubscriptionService subscriptionService)
        {
            _subscriptionBuilder = subscriptionBuilder;
            _subscriptionService = subscriptionService;
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

        public void OnNext(OrderChangedEvent value)
        {
            //Prevent creating subscription for prototype order
            if (value.ChangeState == VirtoCommerce.Platform.Core.Common.EntryState.Added && value.OrigOrder.OperationType != "OrderPrototype")
            {          
                try
                {
                    var subscription = _subscriptionBuilder.CreateSubscriptionFromOrder(value.ModifiedOrder);
                    subscription.CustomerOrderPrototype.OperationType = "OrderPrototype";
                    _subscriptionService.SaveSubscriptions(new[] { subscription });
                }
                catch (Exception ex)
                {
                    throw new CreateSubscriptionException(ex);
                }
            }
        }
    }
}
