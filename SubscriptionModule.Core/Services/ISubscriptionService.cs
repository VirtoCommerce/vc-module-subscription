using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubscriptionModule.Core.Model;
using VirtoCommerce.Domain.Order.Model;

namespace SubscriptionModule.Core.Services
{
    public interface ISubscriptionService
    {
        Subscription[] GetByIds(string[] subscriptionIds, string responseGroup = null);
        void SaveSubscriptions(Subscription[] subscriptions);
        void Delete(string[] ids);
    }
}
