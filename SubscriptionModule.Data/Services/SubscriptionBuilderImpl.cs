using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SubscriptionModule.Core.Model;
using SubscriptionModule.Core.Services;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Platform.Core.Common;

namespace SubscriptionModule.Data.Services
{
    public class SubscriptionBuilderImpl : ISubscriptionBuilder
    {
        private IPaymentPlanService _paymentPlanService;
        public SubscriptionBuilderImpl(IPaymentPlanService paymentPlanService)
        {
            _paymentPlanService = paymentPlanService;
        }

        public Subscription CreateSubscriptionFromOrder(CustomerOrder order)
        {
            Subscription retVal = null;
            //Retrieve one payment plan from customer order items (one simple case, does not handle items with multiple different payment plans in same order)
            var paymentPlanIds = order.Items.Select(x => x.ProductId).Distinct().ToArray();
            var paymentPlan = _paymentPlanService.GetByIds(paymentPlanIds).FirstOrDefault();
            if (paymentPlan != null)
            {
                //There need to make "prototype" for future orders which will be created by subscription schedule information
                retVal = AbstractTypeFactory<Subscription>.TryCreateInstance<Subscription>();
                retVal.StoreId = order.StoreId;
                retVal.CustomerOrderPrototype = GetSubscriptionOrderPrototype(order);
                retVal.CustomerOrdersIds = new[] { order.Id };
                retVal.CustomerId = order.CustomerId;
                retVal.CustomerName = order.CustomerName;
                retVal.Interval = paymentPlan.Interval;
                retVal.IntervalCount = paymentPlan.IntervalCount;
                retVal.TrialPeriodDays = paymentPlan.TrialPeriodDays;
                retVal.SubscriptionStatus = SubscriptionStatus.Active;
            }

            return retVal;
        }

        protected virtual CustomerOrder GetSubscriptionOrderPrototype(CustomerOrder order)
        {
            // without ObjectCreationHandling.Replace default constructor values will be added to result
            var deserializeSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, ObjectCreationHandling = ObjectCreationHandling.Replace };

            var retVal = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(order), order.GetType(), deserializeSettings) as CustomerOrder;
            //Reset all ids
            foreach( var entity in retVal.GetFlatObjectsListWithInterface<IEntity>())
            {
                entity.Id = null;
            }
            return retVal;
        }
    
    }
}
