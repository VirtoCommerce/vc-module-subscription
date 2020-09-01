using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.SubscriptionModule.Core.Model;

namespace VirtoCommerce.SubscriptionModule.Data.Caching
{
    public class PaymentPlanCacheRegion : CancellableCacheRegion<PaymentPlanCacheRegion>
    {
        public static IChangeToken CreateChangeToken(string[] entityIds)
        {
            if (entityIds == null)
            {
                throw new ArgumentNullException(nameof(entityIds));
            }

            var changeTokens = new List<IChangeToken> { CreateChangeToken() };
            foreach (var entityId in entityIds)
            {
                changeTokens.Add(CreateChangeTokenForKey(entityId));
            }
            return new CompositeChangeToken(changeTokens);
        }

        public static void ExpirePaymentPlan(PaymentPlan paymentPlan)
        {
            ExpireTokenForKey(paymentPlan.Id);
        }
    }
}
