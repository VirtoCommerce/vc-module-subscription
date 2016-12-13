using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.Domain.Order.Model;

namespace VirtoCommerce.SubscriptionModule.Core.Services
{
    /// <summary>
    /// Responsible for programmatically working with subscription
    /// </summary>
    public interface ISubscriptionBuilder
    {
        Subscription Subscription { get; }
        /// <summary>
        /// Capture given subscription for future manipulation
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        ISubscriptionBuilder TakeSubscription(Subscription subscription);    
        /// <summary>
        /// Actualize captured subscription (Statuses, Balance etc)
        /// </summary>
        /// <returns></returns>
        ISubscriptionBuilder Actualize();
        /// <summary>
        /// Create new subscription for given customer order with contains items selling by payment plan 
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        Subscription TryCreateSubscriptionFromOrder(CustomerOrder order);
        /// <summary>
        /// Attempt to create new recurrent order with subscription recurring settings
        /// </summary>
        /// <returns></returns>
        CustomerOrder TryToCreateRecurrentOrder();
        void Save();

    }
}
