using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubscriptionModule.Core.Model;
using VirtoCommerce.Domain.Order.Model;

namespace SubscriptionModule.Core.Services
{
    public interface ISubscriptionBuilder
    {
        Subscription CreateSubscriptionFromOrder(CustomerOrder order);
    }
}
