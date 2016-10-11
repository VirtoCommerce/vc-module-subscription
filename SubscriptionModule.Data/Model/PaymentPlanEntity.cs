using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omu.ValueInjecter;
using SubscriptionModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace SubscriptionModule.Data.Model
{
    public class PaymentPlanEntity : AuditableEntity
    {
        [StringLength(64)]
        public string Interval { get; set; }

        public int IntervalCount { get; set; }

        public int TrialPeriodDays { get; set; }

        [StringLength(128)]
        public string ProductId { get; set; }

        public virtual PaymentPlan ToModel(PaymentPlan paymentPlan)
        {
            if (paymentPlan == null)
                throw new ArgumentNullException("paymentPlan");


            paymentPlan.InjectFrom(this);
            paymentPlan.Interval = EnumUtility.SafeParse<PaymentInterval>(this.Interval, PaymentInterval.Months);

            return paymentPlan;
        }

        public virtual PaymentPlanEntity FromModel(PaymentPlan paymentPlan, PrimaryKeyResolvingMap pkMap)
        {
            if (paymentPlan == null)
                throw new ArgumentNullException("paymentPlan");

            pkMap.AddPair(paymentPlan, this);

            this.InjectFrom(paymentPlan);
            this.Interval = paymentPlan.Interval.ToString();
            return this;
        }

        public virtual void Patch(PaymentPlanEntity target)
        {
            target.Interval = this.Interval;
            target.IntervalCount = this.IntervalCount;
            target.ProductId = this.ProductId;
            target.TrialPeriodDays = this.TrialPeriodDays;        
        }

    }
}
