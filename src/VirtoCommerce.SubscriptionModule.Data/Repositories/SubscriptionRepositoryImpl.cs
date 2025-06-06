using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.SubscriptionModule.Data.Model;

namespace VirtoCommerce.SubscriptionModule.Data.Repositories
{
    public class SubscriptionRepositoryImpl : DbContextRepositoryBase<SubscriptionDbContext>, ISubscriptionRepository
    {
        public SubscriptionRepositoryImpl(SubscriptionDbContext context)
            : base(context)
        {
        }

        #region ISubscriptionRepository members    

        public IQueryable<PaymentPlanEntity> PaymentPlans => DbContext.Set<PaymentPlanEntity>();
        public IQueryable<SubscriptionEntity> Subscriptions => DbContext.Set<SubscriptionEntity>();

        public async Task<IList<PaymentPlanEntity>> GetPaymentPlansByIdsAsync(IList<string> ids)
        {
            if (ids.IsNullOrEmpty())
            {
                return [];
            }

            var query = PaymentPlans.Where(x => ids.Contains(x.Id));

            return await query.ToListAsync();
        }

        public async Task<IList<SubscriptionEntity>> GetSubscriptionsByIdsAsync(IList<string> ids, string responseGroup = null)
        {
            var result = await Subscriptions.Where(x => ids.Contains(x.Id)).ToArrayAsync();
            return result;
        }

        #endregion
    }
}
