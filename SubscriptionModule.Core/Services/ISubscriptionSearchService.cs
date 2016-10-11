using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubscriptionModule.Core.Model;
using SubscriptionModule.Core.Model.Search;
using VirtoCommerce.Domain.Commerce.Model.Search;

namespace SubscriptionModule.Core.Services
{
    public interface ISubscriptionSearchService
    {
        GenericSearchResult<Subscription> SearchSubscriptions(SubscriptionSearchCriteria criteria);
    }
}
