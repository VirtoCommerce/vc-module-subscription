using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.Domain.Commerce.Model.Search;

namespace VirtoCommerce.SubscriptionModule.Core.Services
{
    public interface ISubscriptionSearchService
    {
        GenericSearchResult<Subscription> SearchSubscriptions(SubscriptionSearchCriteria criteria);
    }
}
