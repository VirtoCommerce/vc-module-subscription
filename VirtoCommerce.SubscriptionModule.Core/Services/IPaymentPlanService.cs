using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.SubscriptionModule.Core.Model;

namespace VirtoCommerce.SubscriptionModule.Core.Services
{
    public interface IPaymentPlanService
    {
        PaymentPlan[] GetByIds(string[] planIds, string responseGroup = null);
        void SavePlans(PaymentPlan[] plans);
        void Delete(string[] ids);
    }
}
