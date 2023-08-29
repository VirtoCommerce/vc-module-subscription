using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Core;
using VirtoCommerce.SubscriptionModule.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Data.Caching;
using VirtoCommerce.SubscriptionModule.Data.Model;
using VirtoCommerce.SubscriptionModule.Data.Repositories;
using VirtoCommerce.SubscriptionModule.Data.Validation;

namespace VirtoCommerce.SubscriptionModule.Data.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IStoreService _storeService;
        private readonly ICustomerOrderService _customerOrderService;
        private readonly ICustomerOrderSearchService _customerOrderSearchService;
        private readonly Func<ISubscriptionRepository> _subscriptionRepositoryFactory;
        private readonly IUniqueNumberGenerator _uniqueNumberGenerator;
        private readonly IEventPublisher _eventPublisher;
        private readonly IPlatformMemoryCache _platformMemoryCache;
        private readonly ISubscriptionBuilder _subscriptionBuilder;


        public SubscriptionService(
            IStoreService storeService,
            ICustomerOrderService customerOrderService,
            ICustomerOrderSearchService customerOrderSearchService,
            Func<ISubscriptionRepository> subscriptionRepositoryFactory,
            IUniqueNumberGenerator uniqueNumberGenerator,
            IEventPublisher eventPublisher,
            IPlatformMemoryCache platformMemoryCache,
            ISubscriptionBuilder subscriptionBuilder)
        {
            _storeService = storeService;
            _customerOrderService = customerOrderService;
            _customerOrderSearchService = customerOrderSearchService;
            _subscriptionRepositoryFactory = subscriptionRepositoryFactory;
            _uniqueNumberGenerator = uniqueNumberGenerator;
            _eventPublisher = eventPublisher;
            _platformMemoryCache = platformMemoryCache;
            _subscriptionBuilder = subscriptionBuilder;
        }

        #region ISubscriptionService members

        public virtual Task<Subscription[]> GetByIdsAsync(string[] subscriptionIds, string responseGroup = null)
        {
            // complexity checking test
            var cacheKey = CacheKey.With(GetType(), nameof(GetByIdsAsync), string.Join("-", subscriptionIds), responseGroup);
            return _platformMemoryCache.GetOrCreateExclusiveAsync(cacheKey, async cacheEntry =>
            {
                var retVal = new List<Subscription>();
                cacheEntry.AddExpirationToken(SubscriptionCacheRegion.CreateChangeToken(subscriptionIds));

                var subscriptionResponseGroup = EnumUtility.SafeParseFlags(responseGroup, SubscriptionResponseGroup.Full);
                using (var repository = _subscriptionRepositoryFactory())
                {
                    repository.DisableChangesTracking();

                    var subscriptionEntities = await repository.GetSubscriptionsByIdsAsync(subscriptionIds, responseGroup);

                    var subscriptions = subscriptionEntities
                        .Select(x => new
                        {
                            SubscriptionEntity = x,
                            Subscription = AbstractTypeFactory<Subscription>.TryCreateInstance()
                        })
                        .Where(x => x.Subscription != null)
                        .Select(x => x.SubscriptionEntity.ToModel(x.Subscription))
                        .ToList();

                    retVal.AddRange(subscriptions);
                }

                await ProcessSubscriptions(subscriptionIds, subscriptionResponseGroup, retVal);

                return retVal.ToArray();
            });
        }

        public virtual async Task SaveSubscriptionsAsync(Subscription[] subscriptions)
        {
            var pkMap = new PrimaryKeyResolvingMap();
            var changedEntries = new List<GenericChangedEntry<Subscription>>();

            using (var repository = _subscriptionRepositoryFactory())
            {
                var existEntities = await repository.GetSubscriptionsByIdsAsync(subscriptions.Where(x => !x.IsTransient()).Select(x => x.Id).ToArray());
                foreach (var subscription in subscriptions)
                {
                    //Generate numbers for new subscriptions
                    if (string.IsNullOrEmpty(subscription.Number))
                    {
                        var store = await _storeService.GetNoCloneAsync(subscription.StoreId, StoreResponseGroup.StoreInfo.ToString());
                        var numberTemplate = store?.Settings.GetSettingValue(ModuleConstants.Settings.General.NewNumberTemplate.Name, ModuleConstants.Settings.General.NewNumberTemplate.DefaultValue.ToString());
                        subscription.Number = _uniqueNumberGenerator.GenerateNumber(numberTemplate);
                    }
                    //Save subscription order prototype with same as subscription Number
                    if (subscription.CustomerOrderPrototype != null)
                    {
                        subscription.CustomerOrderPrototype.Number = subscription.Number;
                        subscription.CustomerOrderPrototype.IsPrototype = true;
                        await _customerOrderService.SaveChangesAsync(new[] { subscription.CustomerOrderPrototype });
                    }

                    if (subscription.CustomerOrders != null)
                    {
                        foreach (var order in subscription.CustomerOrders)
                        {
                            order.SubscriptionNumber = subscription.Number;
                        }
                        await _customerOrderService.SaveChangesAsync(subscription.CustomerOrders.ToArray());
                    }

                    var originalEntity = existEntities.FirstOrDefault(x => x.Id == subscription.Id);

                    var modifiedEntity = AbstractTypeFactory<SubscriptionEntity>.TryCreateInstance().FromModel(subscription, pkMap);
                    if (originalEntity != null)
                    {
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
                await _eventPublisher.Publish(new SubscriptionChangingEvent(changedEntries));
                await repository.UnitOfWork.CommitAsync();
                pkMap.ResolvePrimaryKeys();

                ClearCacheFor(subscriptions);

                await _eventPublisher.Publish(new SubscriptionChangedEvent(changedEntries));
            }
        }

        public virtual async Task DeleteAsync(string[] ids)
        {
            using (var repository = _subscriptionRepositoryFactory())
            {
                var subscriptions = await GetByIdsAsync(ids);
                if (!subscriptions.IsNullOrEmpty())
                {
                    var changedEntries = subscriptions.Select(x => new GenericChangedEntry<Subscription>(x, EntryState.Deleted)).ToArray();
                    await _eventPublisher.Publish(new SubscriptionChangingEvent(changedEntries));

                    //Remove subscription order prototypes
                    var orderPrototypesIds = repository.Subscriptions.Where(x => ids.Contains(x.Id)).Select(x => x.CustomerOrderPrototypeId).ToArray();
                    await _customerOrderService.DeleteAsync(orderPrototypesIds);

                    await repository.RemoveSubscriptionsByIdsAsync(ids);
                    await repository.UnitOfWork.CommitAsync();

                    ClearCacheFor(subscriptions);

                    await _eventPublisher.Publish(new SubscriptionChangedEvent(changedEntries));
                }
            }
        }

        public async Task<CustomerOrder> CreateOrderForSubscription(Subscription subscription)
        {
            await ValidateSubscription(subscription);
            var subscriptionBuilder = await _subscriptionBuilder.TakeSubscription(subscription).ActualizeAsync();
            var order = await subscriptionBuilder.TryToCreateRecurrentOrderAsync(forceCreation: true);
            await _customerOrderService.SaveChangesAsync(new[] { order });

            ClearCacheFor(new[] { subscription });

            return order;
        }

        #endregion

        protected virtual Task ValidateSubscription(Subscription subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            return ValidateSubscriptionAndThrowAsync(subscription);
        }

        protected async Task ValidateSubscriptionAndThrowAsync(Subscription subscription)
        {
            var validator = new SubscriptionValidator();
            await validator.ValidateAndThrowAsync(subscription);
        }

        protected virtual void ClearCacheFor(Subscription[] subscriptions)
        {
            foreach (var subscription in subscriptions)
            {
                SubscriptionCacheRegion.ExpireSubscription(subscription);
            }

            SubscriptionSearchCacheRegion.ExpireRegion();
        }

        private async Task ProcessSubscriptions(string[] subscriptionIds, SubscriptionResponseGroup subscriptionResponseGroup, List<Subscription> retVal)
        {
            IList<CustomerOrder> orderPrototypes = null;
            IList<CustomerOrder> subscriptionOrders = null;

            if (subscriptionResponseGroup.HasFlag(SubscriptionResponseGroup.WithOrderPrototype))
            {
                orderPrototypes = await _customerOrderService.GetAsync(retVal.Select(x => x.CustomerOrderPrototypeId).ToArray());
            }

            if (subscriptionResponseGroup.HasFlag(SubscriptionResponseGroup.WithRelatedOrders))
            {
                //Loads customer order prototypes and related orders for each subscription via order service
                var criteria = new CustomerOrderSearchCriteria
                {
                    SubscriptionIds = subscriptionIds
                };
                subscriptionOrders = (await _customerOrderSearchService.SearchAsync(criteria)).Results;
            }

            foreach (var subscription in retVal)
            {
                if (!orderPrototypes.IsNullOrEmpty())
                {
                    subscription.CustomerOrderPrototype =
                        orderPrototypes.FirstOrDefault(x => x.Id == subscription.CustomerOrderPrototypeId);
                }

                if (subscriptionOrders.IsNullOrEmpty())
                {
                    continue;
                }

                subscription.CustomerOrders = subscriptionOrders.Where(x => x.SubscriptionId == subscription.Id).ToList();
                subscription.CustomerOrdersIds = subscription.CustomerOrders.Select(x => x.Id).ToArray();
            }
        }
    }
}
