using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.Domain.Commerce.Model;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Payment.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.SubscriptionModule.Data.Services
{
    public class SubscriptionBuilderImpl : ISubscriptionBuilder
    {
        private Subscription _subscription;
        private IPaymentPlanService _paymentPlanService;
        public SubscriptionBuilderImpl(IPaymentPlanService paymentPlanService)
        {
            _paymentPlanService = paymentPlanService;
        }

        #region ISubscriptionBuilder Members
        public virtual Subscription Subscription
        {
            get
            {
                return _subscription;
            }
        }

        public virtual ISubscriptionBuilder Actualize()
        {
            if (!Subscription.IsCancelled)
            {
                //Calculate balance from linked orders
                if (!Subscription.CustomerOrders.IsNullOrEmpty())
                {
                    Subscription.Balance = 0m;
                    var allNotCanceledOrders = Subscription.CustomerOrders.Where(x => !x.IsCancelled);
                    var ordersGrandTotal = allNotCanceledOrders.Sum(x => x.Total);
                    var paidPaymentStatuses = new PaymentStatus[] { PaymentStatus.Authorized, PaymentStatus.Paid };
                    var paidTotal = allNotCanceledOrders.SelectMany(x => x.InPayments).Where(x => !x.IsCancelled && paidPaymentStatuses.Contains(x.PaymentStatus)).Sum(x => x.Sum);

                    Subscription.Balance = ordersGrandTotal - paidTotal;
                }

                //Evaluate current subscription status
                Subscription.SubscriptionStatus = SubscriptionStatus.Active;
                var now = DateTime.UtcNow;
                if(Subscription.TrialSart != null)
                {
                    Subscription.SubscriptionStatus = SubscriptionStatus.Trialing;
                    if (Subscription.TrialEnd != null && now >= Subscription.TrialEnd)
                    {
                        Subscription.SubscriptionStatus = SubscriptionStatus.Active;
                    }
                }

                if(Subscription.SubscriptionStatus != SubscriptionStatus.Trialing && Subscription.Balance > 0)
                {
                    Subscription.SubscriptionStatus = SubscriptionStatus.Unpaid;
                }

                if(Subscription.EndDate != null && now >= Subscription.EndDate)
                {
                    Subscription.IsCancelled = true;
                    Subscription.CancelReason = "Completed with time expiration";
                    Subscription.CancelledDate = now;
                    Subscription.SubscriptionStatus = SubscriptionStatus.Canceled;
                }                
            }
            return this;
        }

        public virtual Subscription TryCreateSubscriptionFromOrder(CustomerOrder order)
        {
            Subscription retVal = null;
            //Retrieve one payment plan from customer order items (one simple case, does not handle items with multiple different payment plans in same order)
            var paymentPlanIds = order.Items.Select(x => x.ProductId).Distinct().ToArray();
            var paymentPlan = _paymentPlanService.GetByIds(paymentPlanIds).FirstOrDefault();
            if (paymentPlan != null)
            {
                var now = DateTime.UtcNow;
                //There need to make "prototype" for future orders which will be created by subscription schedule information
                retVal = AbstractTypeFactory<Subscription>.TryCreateInstance<Subscription>();
                retVal.StoreId = order.StoreId;
                retVal.CustomerOrderPrototype = CloneCustomerOrder(order);
                //Need to prevent subscription creation for prototype order in CreateSubscriptionObserver
                retVal.CustomerOrderPrototype.IsPrototype = true;
                retVal.CustomerId = order.CustomerId;
                retVal.CustomerName = order.CustomerName;
                retVal.Interval = paymentPlan.Interval;
                retVal.IntervalCount = paymentPlan.IntervalCount;
                retVal.StartDate = now;
                retVal.CurrentPeriodStart = now;
                retVal.TrialPeriodDays = paymentPlan.TrialPeriodDays;                
                retVal.SubscriptionStatus = SubscriptionStatus.Active;
                retVal.CurrentPeriodEnd = GetPeriodEnd(now, paymentPlan.Interval, paymentPlan.IntervalCount);
                if(retVal.TrialPeriodDays > 0)
                {
                    retVal.TrialSart = now;
                    retVal.TrialEnd = GetPeriodEnd(now, PaymentInterval.Days, retVal.TrialPeriodDays);
                }
            }

            _subscription = retVal;

            Actualize();

            return retVal;
        }

        public virtual ISubscriptionBuilder TakeSubscription(Subscription subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException("subscription");
            }
            _subscription = subscription;
            return this;
        }

        public virtual CustomerOrder TryToCreateRecurrentOrder(bool forceCreation = false)
        {
            CustomerOrder retVal = null;
            if (!Subscription.IsCancelled)
            {
                var now = DateTime.UtcNow;
                if (forceCreation || now >= Subscription.CurrentPeriodEnd)
                {
                    Subscription.CurrentPeriodStart = now;
                    Subscription.CurrentPeriodEnd = GetPeriodEnd(now, Subscription.Interval, Subscription.IntervalCount);

                    retVal = CloneCustomerOrder(Subscription.CustomerOrderPrototype);
                    retVal.Status = "New";
                    retVal.IsPrototype = false;
                    retVal.SubscriptionId = Subscription.Id;
                    retVal.SubscriptionNumber = Subscription.Number;
                    foreach (var payment in retVal.InPayments)
                    {
                        payment.PaymentStatus = PaymentStatus.New;
                    }
                    foreach (var shipment in retVal.Shipments)
                    {
                        shipment.Status = "New";
                    }
                }           
            }
            return retVal;
        }

        #endregion

        protected virtual CustomerOrder CloneCustomerOrder(CustomerOrder order)
        {
            // without ObjectCreationHandling.Replace default constructor values will be added to result
            var serializationSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, ObjectCreationHandling = ObjectCreationHandling.Replace };

            var retVal = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(order, serializationSettings), order.GetType(), serializationSettings) as CustomerOrder;
            //Reset all ids
            foreach (var entity in retVal.GetFlatObjectsListWithInterface<IEntity>())
            {
                entity.Id = null;
            }
            //Reset all tracking numbers
            foreach (var operation in retVal.GetFlatObjectsListWithInterface<IOperation>())
            {
                operation.Number = null;
                operation.Status = null;                
            }
            return retVal;
        }

        private DateTime GetPeriodEnd(DateTime periodStart, PaymentInterval interval, int intervalCount)
        {
            var retVal = periodStart;
            if(interval == PaymentInterval.Days)
            {
                retVal = retVal.AddDays(Math.Max(1, intervalCount));
            }
            else if(interval == PaymentInterval.Months)
            {
                retVal = retVal.AddMonths(Math.Max(1, intervalCount));
            }
            else if(interval == PaymentInterval.Weeks)
            {
                retVal = retVal.AddYears(Math.Max(1, intervalCount));
            }
            else if(interval == PaymentInterval.Weeks)
            {
                retVal = retVal.AddDays(7 * Math.Max(1, intervalCount));
            }
            return retVal;
        }
    }
}
