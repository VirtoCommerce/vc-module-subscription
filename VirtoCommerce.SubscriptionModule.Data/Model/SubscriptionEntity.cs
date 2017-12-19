using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Omu.ValueInjecter;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SubscriptionModule.Data.Model
{
    public class SubscriptionEntity : AuditableEntity
    {
        public SubscriptionEntity()
        {
        }

        [Required]
        [StringLength(64)]
        public string StoreId { get; set; }

        [Required]
        [StringLength(64)]
        public string CustomerId { get; set; }
        [StringLength(255)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(64)]
        public string Number { get; set; }

        [Column(TypeName = "Money")]
        public decimal Balance { get; set; }

        [StringLength(64)]
        public string Interval { get; set; }

        public int IntervalCount { get; set; }

        public int TrialPeriodDays { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public DateTime? TrialSart { get; set; }
        public DateTime? TrialEnd { get; set; }

        public DateTime? CurrentPeriodStart { get; set; }

        public DateTime? CurrentPeriodEnd { get; set; }

        [StringLength(64)]
        public string Status { get; set; }

        public bool IsCancelled { get; set; }
        public DateTime? CancelledDate { get; set; }
        [StringLength(2048)]
        public string CancelReason { get; set; }

        public string CustomerOrderPrototypeId { get; set; }

        [StringLength(256)]
        public string OuterId { get; set; }

        public virtual Subscription ToModel(Subscription subscription)
        {
            if (subscription == null)
                throw new NullReferenceException("subscription");

            subscription.InjectFrom(this);
   
            subscription.SubscriptionStatus = EnumUtility.SafeParse<SubscriptionStatus>(this.Status, SubscriptionStatus.Active);
            subscription.Interval = EnumUtility.SafeParse<PaymentInterval>(this.Interval, PaymentInterval.Months);
            return subscription;
        }

        public virtual SubscriptionEntity FromModel(Subscription subscription, PrimaryKeyResolvingMap pkMap)
        {          
            if (subscription == null)
                throw new NullReferenceException("subscription");

            pkMap.AddPair(subscription, this);

            this.InjectFrom(subscription);
            if (subscription.CustomerOrderPrototype != null)
            {
                this.CustomerOrderPrototypeId = subscription.CustomerOrderPrototype.Id;
            }
     
            this.Status = subscription.SubscriptionStatus.ToString();
            this.Interval = subscription.Interval.ToString();
            return this;
        }

        public virtual void Patch(SubscriptionEntity target)
        {
            if (target == null)
                throw new NullReferenceException("target");

            target.CustomerOrderPrototypeId = this.CustomerOrderPrototypeId;
            target.CustomerId = this.CustomerId;
            target.CustomerName = this.CustomerName;
            target.StoreId = this.StoreId;
            target.Number = this.Number;
            target.IsCancelled = this.IsCancelled;
            target.CancelledDate = this.CancelledDate;
            target.CancelReason = this.CancelReason;
            target.Status = this.Status;
            target.Interval = this.Interval;
            target.IntervalCount = this.IntervalCount;
            target.TrialPeriodDays = this.TrialPeriodDays;
            target.Balance = this.Balance;
            target.StartDate = this.StartDate;
            target.EndDate = this.EndDate;
            target.TrialSart = this.TrialSart;
            target.TrialEnd = this.TrialEnd;
            target.CurrentPeriodStart = this.CurrentPeriodStart;
            target.CurrentPeriodEnd = this.CurrentPeriodEnd;
            target.OuterId = this.OuterId;
        }

    }
}
