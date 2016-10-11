using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubscriptionModule.Data.Model;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Platform.Core.Common;

namespace SubscriptionModule.Data.Repositories
{
    public interface ISubscriptionRepository : IRepository
    {
        IQueryable<PaymentPlanEntity> PaymentPlans { get; }
        IQueryable<SubscriptionEntity> Subscriptions { get; }
    
        PaymentPlanEntity[] GetPaymentPlansByIds(string[] ids);
        void RemovePaymentPlansByIds(string[] ids);

        SubscriptionEntity[] GetSubscriptionsByIds(string[] ids, string responseGroup = null);
        void RemoveSubscriptionsByIds(string[] ids);
    }
}
