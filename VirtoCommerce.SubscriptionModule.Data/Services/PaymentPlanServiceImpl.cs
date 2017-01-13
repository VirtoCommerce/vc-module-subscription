using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Data.Model;
using VirtoCommerce.SubscriptionModule.Data.Repositories;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.Domain.Commerce.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;

namespace VirtoCommerce.SubscriptionModule.Data.Services
{
    public class PaymentPlanServiceImpl : ServiceBase, IPaymentPlanService, IPaymentPlanSearchService
    {
        private readonly Func<ISubscriptionRepository> _subscriptionRepositoryFactory;
        public PaymentPlanServiceImpl(Func<ISubscriptionRepository> subscriptionRepositoryFactory)
        {
            _subscriptionRepositoryFactory = subscriptionRepositoryFactory;
        }

        #region IPaymentPlanService Members
       
        public PaymentPlan[] GetByIds(string[] planIds, string responseGroup = null)
        {
            var retVal = new List<PaymentPlan>();
         
            using (var repository = _subscriptionRepositoryFactory())
            {
                retVal = repository.GetPaymentPlansByIds(planIds).Select(x=>x.ToModel(AbstractTypeFactory<PaymentPlan>.TryCreateInstance())).ToList();             
            }
            return retVal.ToArray();
        }

        public void SavePlans(PaymentPlan[] plans)
        {
            var pkMap = new PrimaryKeyResolvingMap();
            using (var repository = _subscriptionRepositoryFactory())
            using (var changeTracker = GetChangeTracker(repository))
            {
                var existPlanEntities = repository.GetPaymentPlansByIds(plans.Where(x => !x.IsTransient()).Select(x => x.Id).ToArray());
                foreach (var paymentPlan in plans)
                {                 
                    var sourcePlanEntity = AbstractTypeFactory<PaymentPlanEntity>.TryCreateInstance();
                    if (sourcePlanEntity != null)
                    {
                        sourcePlanEntity = sourcePlanEntity.FromModel(paymentPlan, pkMap) as PaymentPlanEntity;
                        var targetPlanEntity = existPlanEntities.FirstOrDefault(x => x.Id == paymentPlan.Id);
                        if (targetPlanEntity != null)
                        {
                            changeTracker.Attach(targetPlanEntity);
                            sourcePlanEntity.Patch(targetPlanEntity);
                        }
                        else
                        {
                            repository.Add(sourcePlanEntity);
                        }
                    }
                }

                CommitChanges(repository);
                pkMap.ResolvePrimaryKeys();
            }
        }

        public void Delete(string[] ids)
        {
            using (var repository = _subscriptionRepositoryFactory())
            {
                repository.RemovePaymentPlansByIds(ids);
                CommitChanges(repository);
            }
        }

        #endregion


        #region IPaymentPlanSearchService members
        public GenericSearchResult<PaymentPlan> SearchPlans(PaymentPlanSearchCriteria criteria)
        {
            var retVal = new GenericSearchResult<PaymentPlan>();
            using (var repository = _subscriptionRepositoryFactory())
            {
                var query = repository.PaymentPlans;           

                var sortInfos = criteria.SortInfos;
                if (sortInfos.IsNullOrEmpty())
                {
                    sortInfos = new[] { new SortInfo { SortColumn = ReflectionUtility.GetPropertyName<PaymentPlan>(x => x.CreatedDate), SortDirection = SortDirection.Descending } };
                }
                query = query.OrderBySortInfos(sortInfos);

                retVal.TotalCount = query.Count();

                var paymentPlanIds = query.Skip(criteria.Skip)
                                            .Take(criteria.Take)
                                            .ToArray()
                                            .Select(x => x.Id)
                                            .ToArray();

                //Load subscriptions with preserving sorting order
                retVal.Results = GetByIds(paymentPlanIds, criteria.ResponseGroup).OrderBy(x => Array.IndexOf(paymentPlanIds, x.Id)).ToArray();
                return retVal;
            }
        } 
        #endregion

    }
}
