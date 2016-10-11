using System;
using System.Collections.Generic;
using System.Linq;
using SubscriptionModule.Core.Model;
using SubscriptionModule.Core.Model.Search;
using SubscriptionModule.Core.Services;
using SubscriptionModule.Data.Model;
using SubscriptionModule.Data.Repositories;
using VirtoCommerce.Domain.Commerce.Model.Search;
using VirtoCommerce.Domain.Common;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Infrastructure;

namespace SubscriptionModule.Data.Services
{
    public class SubscriptionServiceImpl : ServiceBase, ISubscriptionService, ISubscriptionSearchService
    {
        private readonly IStoreService _storeService;
        private readonly ICustomerOrderService _customerOrderService;
        private readonly Func<ISubscriptionRepository> _subscriptionRepositoryFactory;
        private readonly IUniqueNumberGenerator _uniqueNumberGenerator;

        public SubscriptionServiceImpl(Func<ISubscriptionRepository> subscriptionRepositoryFactory, ICustomerOrderService customerOrderService, IStoreService storeService, IUniqueNumberGenerator uniqueNumberGenerator)
        {
            _subscriptionRepositoryFactory = subscriptionRepositoryFactory;
            _customerOrderService = customerOrderService;
            _storeService = storeService;
            _uniqueNumberGenerator = uniqueNumberGenerator;
        }

        #region ISubscriptionService members

        public Subscription[] GetByIds(string[] subscriptionIds, string responseGroup = null)
        {
            var retVal = new List<Subscription>();
            var orderResponseGroup = EnumUtility.SafeParse(responseGroup, CustomerOrderResponseGroup.Full);
            using (var repository = _subscriptionRepositoryFactory())
            {
                var subscriptionEntities = repository.GetSubscriptionsByIds(subscriptionIds, responseGroup);
                foreach (var subscriptionEntity in subscriptionEntities)
                {
                    var subscription = AbstractTypeFactory<Subscription>.TryCreateInstance();
                    if (subscription != null)
                    {
                        subscription = subscriptionEntity.ToModel(subscription) as Subscription;                    
                        retVal.Add(subscription);
                    }
                }
            }
            //Loads customer order prototypes and related orders for each subscription via order service
            var orderIds = retVal.Select(x => x.CustomerOrderPrototypeId).Distinct().Concat(retVal.SelectMany(x => x.CustomerOrdersIds)).Distinct().ToArray();
            var orders = _customerOrderService.GetByIds(orderIds, responseGroup);
            foreach(var subscription in retVal)
            {
                subscription.CustomerOrderPrototype = orders.FirstOrDefault(x => x.Id == subscription.CustomerOrderPrototypeId);
                subscription.CustomerOrders = orders.Where(x => subscription.CustomerOrdersIds.Contains(x.Id)).ToList();
            }         
            return retVal.ToArray();
        }

        public void SaveSubscriptions(Subscription[] subscriptions)
        {
            var pkMap = new PrimaryKeyResolvingMap();

            using (var repository = _subscriptionRepositoryFactory())
            using (var changeTracker = GetChangeTracker(repository))
            {
                //Save order prototypes separately via order service
                var orderPrototypes = subscriptions.Where(x=>x.CustomerOrderPrototype != null).Select(x => x.CustomerOrderPrototype).ToArray();
                _customerOrderService.SaveChanges(orderPrototypes);

                var dataExistSubscriptions = repository.GetSubscriptionsByIds(subscriptions.Where(x => !x.IsTransient()).Select(x => x.Id).ToArray());
                foreach (var subscription in subscriptions)
                {
                    //Generate numbers for new subscriptions
                    if(string.IsNullOrEmpty(subscription.Number))
                    {
                        var store = _storeService.GetById(subscription.StoreId);
                        var numberTemplate = store.Settings.GetSettingValue("Subscription.SubscriptionNewNumberTemplate", "SU{0:yyMMdd}-{1:D5}");
                        subscription.Number = _uniqueNumberGenerator.GenerateNumber(numberTemplate);
                    }

                    var dataSourceSubscription = AbstractTypeFactory<SubscriptionEntity>.TryCreateInstance();
                    if (dataSourceSubscription != null)
                    {
                        dataSourceSubscription = dataSourceSubscription.FromModel(subscription, pkMap) as SubscriptionEntity;
                        var dataTargetSubscription = dataExistSubscriptions.FirstOrDefault(x => x.Id == subscription.Id);
                        if (dataTargetSubscription != null)
                        {                           
                            changeTracker.Attach(dataTargetSubscription);
                            dataSourceSubscription.Patch(dataTargetSubscription);                          
                        }
                        else
                        {
                            repository.Add(dataSourceSubscription);                            
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
                repository.RemoveSubscriptionsByIds(ids);
                CommitChanges(repository);
            }
        }
        #endregion

        #region ISubscriptionSearchService members
        public GenericSearchResult<Subscription> SearchSubscriptions(SubscriptionSearchCriteria criteria)
        {
            var retVal = new GenericSearchResult<Subscription>();
            using (var repository = _subscriptionRepositoryFactory())
            {
                var query = repository.Subscriptions;

                if (!string.IsNullOrEmpty(criteria.Number))
                {
                    query = query.Where(x => x.Number == criteria.Number);
                }             
                if (criteria.CustomerId != null)
                {
                    query = query.Where(x => x.CustomerId == criteria.CustomerId);
                }
                if (criteria.Statuses != null && criteria.Statuses.Any())
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

                var sortInfos = criteria.SortInfos;
                if (sortInfos.IsNullOrEmpty())
                {
                    sortInfos = new[] { new SortInfo { SortColumn = ReflectionUtility.GetPropertyName<Subscription>(x => x.CreatedDate), SortDirection = SortDirection.Descending } };
                }
                query = query.OrderBySortInfos(sortInfos);

                retVal.TotalCount = query.Count();

                var subscriptions = query.Skip(criteria.Skip).Take(criteria.Take).ToArray().Select(x=> x.ToModel(AbstractTypeFactory<Subscription>.TryCreateInstance())).ToList();
                retVal.Results = subscriptions;
                return retVal;
            }
        }
        #endregion

    

    }
}
