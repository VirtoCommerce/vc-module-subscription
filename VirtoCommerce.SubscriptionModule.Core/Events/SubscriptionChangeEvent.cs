using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SubscriptionModule.Core.Model;

namespace VirtoCommerce.SubscriptionModule.Core.Events
{
    public class SubscriptionChangeEvent
    {
        public SubscriptionChangeEvent(EntryState state, Subscription originalSubscription, Subscription modifiedSubscription)
        {
            ChangeState = state;
            OriginalSubscription = originalSubscription;
            ModifiedSubscription = modifiedSubscription;
        }

        public EntryState ChangeState { get; set; }
        public Subscription OriginalSubscription { get; set; }
        public Subscription ModifiedSubscription { get; set; }
    }

}
