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

public class PaymentPlanSearchService(Func<ISubscriptionRepository> repositoryFactory, IPlatformMemoryCache platformMemoryCache, IPaymentPlanService crudService, IOptions<CrudOptions> crudOptions)
: SearchService<PaymentPlanSearchCriteria, PaymentPlanSearchResult, PaymentPlan, PaymentPlanEntity>
    (repositoryFactory, platformMemoryCache, crudService, crudOptions),
    IPaymentPlanSearchService

{
    protected override IQueryable<PaymentPlanEntity> BuildQuery(IRepository repository, PaymentPlanSearchCriteria criteria)
    {
        var query = ((ISubscriptionRepository)repository).PaymentPlans;
        return query;
    }

    protected override IList<SortInfo> BuildSortExpression(PaymentPlanSearchCriteria criteria)
    {
        var sortInfos = criteria.SortInfos;

        if (sortInfos.IsNullOrEmpty())
        {
            sortInfos =
            [
                new SortInfo { SortColumn = nameof(PaymentPlan.CreatedDate), SortDirection = SortDirection.Descending }
            ];
        }

        return sortInfos;
    }
}
