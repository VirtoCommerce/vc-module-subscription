using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Commerce.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;

namespace VirtoCommerce.SubscriptionModule.Core.Services
{
    public interface IPaymentPlanSearchService
    {
        GenericSearchResult<PaymentPlan> SearchPlans(PaymentPlanSearchCriteria criteria);
    }
}
