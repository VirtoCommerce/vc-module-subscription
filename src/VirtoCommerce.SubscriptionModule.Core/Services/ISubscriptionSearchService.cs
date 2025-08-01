using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;

namespace VirtoCommerce.SubscriptionModule.Core.Services
{
    public interface ISubscriptionSearchService : ISearchService<SubscriptionSearchCriteria, SubscriptionSearchResult, Subscription>
    {
    }
}
