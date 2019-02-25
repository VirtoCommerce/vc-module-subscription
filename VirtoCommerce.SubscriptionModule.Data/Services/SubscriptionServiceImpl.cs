using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Data.Model;
using VirtoCommerce.SubscriptionModule.Data.Repositories;
using VirtoCommerce.Domain.Commerce.Model.Search;
using VirtoCommerce.Domain.Common;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Events;
using VirtoCommerce.Domain.Common.Events;

namespace VirtoCommerce.SubscriptionModule.Data.Services
{
    public class SubscriptionServiceImpl : ServiceBase, ISubscriptionService, ISubscriptionSearchService
    {
        private readonly IStoreService _storeService;
        private readonly ICustomerOrderService _customerOrderService;
        private readonly ICustomerOrderSearchService _customerOrderSearchService;
        private readonly Func<ISubscriptionRepository> _subscriptionRepositoryFactory;
        private readonly IUniqueNumberGenerator _uniqueNumberGenerator;
        private readonly IChangeLogService _changeLogService;
        private readonly IEventPublisher _eventPublisher;

        public SubscriptionServiceImpl(Func<ISubscriptionRepository> subscriptionRepositoryFactory, ICustomerOrderService customerOrderService, ICustomerOrderSearchService customerOrderSearchService,
                                       IStoreService storeService, IUniqueNumberGenerator uniqueNumberGenerator, IChangeLogService changeLogService, IEventPublisher eventPublisher)
        {
            _customerOrderSearchService = customerOrderSearchService;
            _subscriptionRepositoryFactory = subscriptionRepositoryFactory;
            _customerOrderService = customerOrderService;
            _storeService = storeService;
            _uniqueNumberGenerator = uniqueNumberGenerator;
            _changeLogService = changeLogService;
            _eventPublisher = eventPublisher;
        }

        #region ISubscriptionService members

        public Subscription[] GetByIds(string[] subscriptionIds, string responseGroup = null)
        {
            var retVal = new List<Subscription>();
            var subscriptionResponseGroup = EnumUtility.SafeParse(responseGroup, SubscriptionResponseGroup.Full);
            using (var repository = _subscriptionRepositoryFactory())
            {
                repository.DisableChangesTracking();

                var subscriptionEntities = repository.GetSubscriptionsByIds(subscriptionIds, responseGroup);
                foreach (var subscriptionEntity in subscriptionEntities)
                {
                    var subscription = AbstractTypeFactory<Subscription>.TryCreateInstance();
                    if (subscription != null)
                    {
                        subscription = subscriptionEntity.ToModel(subscription) as Subscription;
                        if (subscriptionResponseGroup.HasFlag(SubscriptionResponseGroup.WithChangeLog))
                        {
                            //Load change log by separate request
                            _changeLogService.LoadChangeLogs(subscription);
                        }
                        retVal.Add(subscription);
                    }
                }
            }

            CustomerOrder[] orderPrototypes = null;
            CustomerOrder[] subscriptionOrders = null;

            if (subscriptionResponseGroup.HasFlag(SubscriptionResponseGroup.WithOrderPrototype))
            {
                orderPrototypes = _customerOrderService.GetByIds(retVal.Select(x => x.CustomerOrderPrototypeId).ToArray());
            }
            if (subscriptionResponseGroup.HasFlag(SubscriptionResponseGroup.WithRelatedOrders))
            {
                //Loads customer order prototypes and related orders for each subscription via order service
                var criteria = new CustomerOrderSearchCriteria
                {
                    SubscriptionIds = subscriptionIds
                };
                subscriptionOrders = _customerOrderSearchService.SearchCustomerOrders(criteria).Results.ToArray();
            }

            foreach (var subscription in retVal)
            {
                if (!orderPrototypes.IsNullOrEmpty())
                {
                    subscription.CustomerOrderPrototype = orderPrototypes.FirstOrDefault(x => x.Id == subscription.CustomerOrderPrototypeId);
                }
                if (!subscriptionOrders.IsNullOrEmpty())
                {
                    subscription.CustomerOrders = subscriptionOrders.Where(x => x.SubscriptionId == subscription.Id).ToList();
                    subscription.CustomerOrdersIds = subscription.CustomerOrders.Select(x => x.Id).ToArray();
                }
            }

            return retVal.ToArray();
        }

        public void SaveSubscriptions(Subscription[] subscriptions)
        {
            var pkMap = new PrimaryKeyResolvingMap();
            var changedEntries = new List<GenericChangedEntry<Subscription>>();

            using (var repository = _subscriptionRepositoryFactory())
            using (var changeTracker = GetChangeTracker(repository))
            {
                var existEntities = repository.GetSubscriptionsByIds(subscriptions.Where(x => !x.IsTransient()).Select(x => x.Id).ToArray());
                foreach (var subscription in subscriptions)
                {
                    //Generate numbers for new subscriptions
                    if (string.IsNullOrEmpty(subscription.Number))
                    {
                        var store = _storeService.GetById(subscription.StoreId);
                        var numberTemplate = store.Settings.GetSettingValue("Subscription.SubscriptionNewNumberTemplate", "SU{0:yyMMdd}-{1:D5}");
                        subscription.Number = _uniqueNumberGenerator.GenerateNumber(numberTemplate);
                    }
                    //Save subscription order prototype with same as subscription Number
                    if (subscription.CustomerOrderPrototype != null)
                    {
                        subscription.CustomerOrderPrototype.Number = subscription.Number;
                        subscription.CustomerOrderPrototype.IsPrototype = true;
                        _customerOrderService.SaveChanges(new[] { subscription.CustomerOrderPrototype });
                    }
                    var originalEntity = existEntities.FirstOrDefault(x => x.Id == subscription.Id);
                    var originalSubscription = originalEntity != null ? originalEntity.ToModel(AbstractTypeFactory<Subscription>.TryCreateInstance()) : subscription;

                    var modifiedEntity = AbstractTypeFactory<SubscriptionEntity>.TryCreateInstance()
                                                                                 .FromModel(subscription, pkMap) as SubscriptionEntity;
                    if (originalEntity != null)
                    {
                        changeTracker.Attach(originalEntity);
                        changedEntries.Add(new GenericChangedEntry<Subscription>(subscription, originalEntity.ToModel(AbstractTypeFactory<Subscription>.TryCreateInstance()), EntryState.Modified));
                        modifiedEntity.Patch(originalEntity);
                        //force the subscription.ModifiedDate update, because the subscription object may not have any changes in its properties
                        originalEntity.ModifiedDate = DateTime.UtcNow;
                    }
                    else
                    {
                        repository.Add(modifiedEntity);
                        changedEntries.Add(new GenericChangedEntry<Subscription>(subscription, EntryState.Added));
                    }
                }

                //Raise domain events
                _eventPublisher.Publish(new SubscriptionChangingEvent(changedEntries));
                CommitChanges(repository);
                pkMap.ResolvePrimaryKeys();
                _eventPublisher.Publish(new SubscriptionChangedEvent(changedEntries));
            }
        }

        public void Delete(string[] ids)
        {
            using (var repository = _subscriptionRepositoryFactory())
            {
                var subscriptions = GetByIds(ids);
                if (!subscriptions.IsNullOrEmpty())
                {
                    var changedEntries = subscriptions.Select(x => new GenericChangedEntry<Subscription>(x, EntryState.Deleted));
                    _eventPublisher.Publish(new SubscriptionChangingEvent(changedEntries));

                    //Remove subscription order prototypes
                    var orderPrototypesIds = repository.Subscriptions.Where(x => ids.Contains(x.Id)).Select(x => x.CustomerOrderPrototypeId).ToArray();
                    _customerOrderService.Delete(orderPrototypesIds);

                    repository.RemoveSubscriptionsByIds(ids);
                    CommitChanges(repository);

                    _eventPublisher.Publish(new SubscriptionChangedEvent(changedEntries));
                }
            }
        }
        #endregion

        #region ISubscriptionSearchService members
        public GenericSearchResult<Subscription> SearchSubscriptions(SubscriptionSearchCriteria criteria)
        {
            var retVal = new GenericSearchResult<Subscription>();
            using (var repository = _subscriptionRepositoryFactory())
            {
                repository.DisableChangesTracking();

                var query = repository.Subscriptions;

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

                if (criteria.ModifiedSinceDate != null)
                {
                    query = query.Where(x => x.ModifiedDate >= criteria.ModifiedSinceDate);
                }

                if (!string.IsNullOrEmpty(criteria.CustomerOrderId))
                {
                    var order = _customerOrderService.GetByIds(new[] { criteria.CustomerOrderId }).FirstOrDefault();
                    if (order != null && !string.IsNullOrEmpty(order.SubscriptionId))
                    {
                        query = query.Where(x => x.Id == order.SubscriptionId);
                    }
                    else
                    {
                        query = query.Where(x => false);
                    }

                }

                if (criteria.OuterId != null)
                {
                    query = query.Where(x => x.OuterId == criteria.OuterId);
                }

                var sortInfos = criteria.SortInfos;
                if (sortInfos.IsNullOrEmpty())
                {
                    sortInfos = new[] { new SortInfo { SortColumn = ReflectionUtility.GetPropertyName<Subscription>(x => x.CreatedDate), SortDirection = SortDirection.Descending } };
                }
                query = query.OrderBySortInfos(sortInfos);

                retVal.TotalCount = query.Count();

                var subscriptionsIds = query.Skip(criteria.Skip)
                                            .Take(criteria.Take)
                                            .ToArray()
                                            .Select(x => x.Id)
                                            .ToArray();

                //Load subscriptions with preserving sorting order
                retVal.Results = GetByIds(subscriptionsIds, criteria.ResponseGroup).OrderBy(x => Array.IndexOf(subscriptionsIds, x.Id)).ToArray();
                return retVal;
            }
        }
        #endregion
    }
}
