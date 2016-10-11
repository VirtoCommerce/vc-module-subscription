using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubscriptionModule.Core.Model
{
    public enum SubscriptionStatus
    {
        Active,
        Trialing,
        PastDue,
        Canceled,
        Unpaid
    }
}
