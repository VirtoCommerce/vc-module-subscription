using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Events;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.SubscriptionModule.Data.Caching;

namespace VirtoCommerce.SubscriptionModule.Data.Handlers
{
    public class InvalidateCacheOrderChangedEventHandler : IEventHandler<OrderChangedEvent>
    {
        public virtual Task Handle(OrderChangedEvent message)
        {
            var deletedSubscriptionOrders = message
                .ChangedEntries
                .Where(x => x.EntryState == EntryState.Deleted
                            && !x.NewEntry.SubscriptionId.IsNullOrEmpty()
                            && !x.NewEntry.IsPrototype)
                .Select(e => e.NewEntry);

            foreach (var order in deletedSubscriptionOrders)
            {
                SubscriptionCacheRegion.ExpireTokenForKey(order.SubscriptionId);
            }

            return Task.CompletedTask;
        }
    }
}
