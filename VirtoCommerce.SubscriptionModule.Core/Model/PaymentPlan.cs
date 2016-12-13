using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Catalog.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SubscriptionModule.Core.Model
{
    public class PaymentPlan : AuditableEntity 
    {
        public PaymentPlan()
        {
            Interval = PaymentInterval.Months;
        }
        /// <summary>
        /// (days, months, years) - billing interval
        /// </summary>
        public PaymentInterval Interval { get; set; }
        /// <summary>
        /// - to set more customized intervals (every 5 month)
        /// </summary>
        public int IntervalCount { get; set; }
        /// <summary>
        ///  subscription trial period in days 
        /// </summary>
        public int TrialPeriodDays { get; set; }
    }
}
