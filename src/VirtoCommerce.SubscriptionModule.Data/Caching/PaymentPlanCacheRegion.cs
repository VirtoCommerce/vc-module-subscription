using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Primitives;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.SubscriptionModule.Core.Model;

namespace VirtoCommerce.SubscriptionModule.Data.Caching
{
    public class PaymentPlanCacheRegion : CancellableCacheRegion<PaymentPlanCacheRegion>
    {
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _entityRegionTokenLookup = new ConcurrentDictionary<string, CancellationTokenSource>();

        public static IChangeToken CreateChangeToken(string[] entityIds)
        {
            if (entityIds == null)
            {
                throw new ArgumentNullException(nameof(entityIds));
            }

            var changeTokens = new List<IChangeToken> { CreateChangeToken() };
            foreach (var entityId in entityIds.Where(x => x != null))
            {
                changeTokens.Add(new CancellationChangeToken(_entityRegionTokenLookup.GetOrAdd(entityId, new CancellationTokenSource()).Token));
            }
            return new CompositeChangeToken(changeTokens);
        }

        public static void ExpirePaymentPlan(PaymentPlan paymentPlan)
        {
            if (_entityRegionTokenLookup.TryRemove(paymentPlan.Id, out var token))
            {
                token.Cancel();
            }
        }
    }
}
