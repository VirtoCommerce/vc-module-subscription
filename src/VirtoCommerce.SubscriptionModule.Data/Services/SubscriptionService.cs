using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Data.GenericCrud;
using VirtoCommerce.SubscriptionModule.Core;
using VirtoCommerce.SubscriptionModule.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Data.Model;
using VirtoCommerce.SubscriptionModule.Data.Repositories;
using VirtoCommerce.SubscriptionModule.Data.Validation;

namespace VirtoCommerce.SubscriptionModule.Data.Services;

public class SubscriptionService(Func<ISubscriptionRepository> subscriptionRepositoryFactory,
        IEventPublisher eventPublisher,
        IPlatformMemoryCache platformMemoryCache,
        ICustomerOrderService customerOrderService,
        ICustomerOrderSearchService customerOrderSearchService,
        ISubscriptionBuilder subscriptionBuilder) : CrudService<Subscription, SubscriptionEntity, SubscriptionChangingEvent, SubscriptionChangedEvent>(subscriptionRepositoryFactory, platformMemoryCache, eventPublisher), ISubscriptionService
{
    private readonly ICustomerOrderService _customerOrderService = customerOrderService;
    private readonly ICustomerOrderSearchService _customerOrderSearchService = customerOrderSearchService;
    private readonly ISubscriptionBuilder _subscriptionBuilder = subscriptionBuilder;

    protected override async Task BeforeSaveChanges(IList<Subscription> models)
    {
        var customerOrderPrototypes = models.Where(x => x.CustomerOrderPrototype != null)
                .Select(x => x.CustomerOrderPrototype).ToList();

        if (customerOrderPrototypes.IsNullOrEmpty())
        {
            await _customerOrderService.SaveChangesAsync(customerOrderPrototypes);
        }

        await base.BeforeSaveChanges(models);
    }

    protected override Task<IList<SubscriptionEntity>> LoadEntities(IRepository repository, IList<string> ids, string responseGroup)
    {
        return ((ISubscriptionRepository)repository).GetSubscriptionsByIdsAsync(ids, responseGroup);
    }

    protected override IList<Subscription> ProcessModels(IList<SubscriptionEntity> entities, string responseGroup)
    {
        var subscriptions = base.ProcessModels(entities, responseGroup);

        if (subscriptions.IsNullOrEmpty())
        {
            return [];
        }

        return ProcessSubscriptions(subscriptions, EnumUtility.SafeParseFlags(responseGroup, SubscriptionResponseGroup.Full)).GetAwaiter().GetResult();

    }

    public async Task<CustomerOrder> CreateOrderForSubscription(Subscription subscription)
    {
        await ValidateSubscription(subscription);

        var builder = await _subscriptionBuilder.TakeSubscription(subscription).ActualizeAsync();
        var order = await builder.TryToCreateRecurrentOrderAsync(forceCreation: true);

        if (order == null)
        {
            throw new SubscriptionException($"Cannot create order for subscription with id {subscription.Id}. Subscription is not active or has no payment plan.");
        }

        await _customerOrderService.SaveChangesAsync([order]);

        return order;
    }


    protected virtual Task ValidateSubscription(Subscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        return ValidateSubscriptionAndThrowAsync(subscription);
    }

    protected Task ValidateSubscriptionAndThrowAsync(Subscription subscription)
    {
        var validator = new SubscriptionValidator();
        return validator.ValidateAndThrowAsync(subscription);
    }


    private async Task<IList<Subscription>> ProcessSubscriptions(IList<Subscription> subscriptions, SubscriptionResponseGroup subscriptionResponseGroup)
    {
        IList<CustomerOrder> orderPrototypes = null;
        IList<CustomerOrder> subscriptionOrders = null;

        if (subscriptionResponseGroup.HasFlag(SubscriptionResponseGroup.WithOrderPrototype))
        {
            var orderIds = subscriptions.Where(x => x.CustomerOrderPrototypeId != null).Select(x => x.CustomerOrderPrototypeId).ToList();
            if (!orderIds.IsNullOrEmpty())
            {
                orderPrototypes = await _customerOrderService.GetAsync(subscriptions.Where(x => x.CustomerOrderPrototypeId != null).Select(x => x.CustomerOrderPrototypeId).ToList());
            }
        }

        if (subscriptionResponseGroup.HasFlag(SubscriptionResponseGroup.WithRelatedOrders))
        {
            //Loads customer order prototypes and related orders for each subscription via order service
            var criteria = new CustomerOrderSearchCriteria
            {
                SubscriptionIds = subscriptions.Select(x => x.Id).ToArray(),
            };
            subscriptionOrders = (await _customerOrderSearchService.SearchAllAsync(criteria));
        }

        foreach (var subscription in subscriptions)
        {
            if (!orderPrototypes.IsNullOrEmpty())
            {
                subscription.CustomerOrderPrototype =
                    orderPrototypes.FirstOrDefault(x => x.Id == subscription.CustomerOrderPrototypeId);
            }

            if (!subscriptionOrders.IsNullOrEmpty())
            {
                subscription.CustomerOrders = subscriptionOrders.Where(x => x.SubscriptionId == subscription.Id).ToList();
                subscription.CustomerOrdersIds = subscription.CustomerOrders.Select(x => x.Id).ToArray();
            }
        }

        return subscriptions;
    }
}
