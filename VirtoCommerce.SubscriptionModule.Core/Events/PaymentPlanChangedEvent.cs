using System.Collections.Generic;
using VirtoCommerce.Domain.Common.Events;
using VirtoCommerce.SubscriptionModule.Core.Model;

namespace VirtoCommerce.SubscriptionModule.Core.Events
{
    public class PaymentPlanChangedEvent : GenericChangedEntryEvent<PaymentPlan>
    {
        public PaymentPlanChangedEvent(IEnumerable<GenericChangedEntry<PaymentPlan>> changedEntries)
           : base(changedEntries)
        {
        }
    }
}
