using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtoCommerce.SubscriptionModule.Core.Model
{
    [Flags]
    public enum SubscriptionResponseGroup
    {
        Default,
        WithOrderPrototype,
        WithRlatedOrders,
        Full = Default | WithOrderPrototype | WithRlatedOrders        
    }
}
