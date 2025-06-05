using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.SubscriptionModule.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Data.Model;
using VirtoCommerce.SubscriptionModule.Data.Repositories;

namespace VirtoCommerce.SubscriptionModule.Data.Services
{
    public class PaymentPlanService(Func<ISubscriptionRepository> subscriptionRepositoryFactory, IEventPublisher eventPublisher, IPlatformMemoryCache platformMemoryCache) : CrudService<PaymentPlan, PaymentPlanEntity, PaymentPlanChangingEvent, PaymentPlanChangedEvent>(subscriptionRepositoryFactory, platformMemoryCache, eventPublisher), IPaymentPlanService
    {
        public async Task<PaymentPlan[]> GetByIdsAsync(string[] planIds, string responseGroup = null)
        {
            var resut = await base.GetAsync(planIds, responseGroup);
            return resut.ToArray();
        }

        protected override Task<IList<PaymentPlanEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
        {
            return ((ISubscriptionRepository)repository).GetPaymentPlansByIdsAsync(ids);
        }
    }
}
