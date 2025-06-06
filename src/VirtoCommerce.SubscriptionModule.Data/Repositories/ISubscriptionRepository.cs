using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SubscriptionModule.Data.Model;

namespace VirtoCommerce.SubscriptionModule.Data.Repositories
{
    public interface ISubscriptionRepository : IRepository
    {
        IQueryable<PaymentPlanEntity> PaymentPlans { get; }
        IQueryable<SubscriptionEntity> Subscriptions { get; }

        Task<IList<PaymentPlanEntity>> GetPaymentPlansByIdsAsync(IList<string> ids);

        Task<IList<SubscriptionEntity>> GetSubscriptionsByIdsAsync(IList<string> ids, string responseGroup = null);
    }
}
