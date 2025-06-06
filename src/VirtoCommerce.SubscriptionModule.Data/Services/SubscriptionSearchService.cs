using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Data.Model;
using VirtoCommerce.SubscriptionModule.Data.Repositories;

namespace VirtoCommerce.SubscriptionModule.Data.Services;

public class SubscriptionSearchService(
    Func<ISubscriptionRepository> repositoryFactory,
    IPlatformMemoryCache platformMemoryCache,
    ISubscriptionService crudService,
    IOptions<CrudOptions> crudOptions)
    : SearchService<SubscriptionSearchCriteria, SubscriptionSearchResult, Subscription, SubscriptionEntity>
        (repositoryFactory, platformMemoryCache, crudService, crudOptions),
        ISubscriptionSearchService
{
    protected override IQueryable<SubscriptionEntity> BuildQuery(IRepository repository, SubscriptionSearchCriteria criteria)
    {
        var query = ((ISubscriptionRepository)repository).Subscriptions;

        if (!string.IsNullOrEmpty(criteria.Number))
        {
            query = query.Where(x => x.Number == criteria.Number);
        }
        else if (criteria.Keyword != null)
        {
            query = query.Where(x => x.Number.Contains(criteria.Keyword));
        }

        if (criteria.CustomerId != null)
        {
            query = query.Where(x => x.CustomerId == criteria.CustomerId);
        }

        if (!criteria.Statuses.IsNullOrEmpty())
        {
            query = query.Where(x => criteria.Statuses.Contains(x.Status));
        }

        if (criteria.StoreId != null)
        {
            query = query.Where(x => criteria.StoreId == x.StoreId);
        }

        if (criteria.StartDate != null)
        {
            query = query.Where(x => x.CreatedDate >= criteria.StartDate);
        }

        if (criteria.EndDate != null)
        {
            query = query.Where(x => x.CreatedDate <= criteria.EndDate);
        }

        if (criteria.ModifiedSinceDate != null)
        {
            query = query.Where(x => x.ModifiedDate >= criteria.ModifiedSinceDate);
        }

        if (criteria.OuterId != null)
        {
            query = query.Where(x => x.OuterId == criteria.OuterId);
        }

        return query;
    }

    protected override IList<SortInfo> BuildSortExpression(SubscriptionSearchCriteria criteria)
    {
        var sortInfos = criteria.SortInfos;

        if (sortInfos.IsNullOrEmpty())
        {
            sortInfos =
            [
                new SortInfo { SortColumn = nameof(SubscriptionEntity.CreatedDate), SortDirection = SortDirection.Descending },
            ];
        }

        return sortInfos;
    }
}
