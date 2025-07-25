using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SubscriptionModule.Core;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Web.Model;

namespace VirtoCommerce.SubscriptionModule.Web.Controllers.Api
{
    [Route("api/subscriptions")]
    [Authorize]
    public class SubscriptionModuleController(
        ISubscriptionService subscriptionService,
        ISubscriptionSearchService subscriptionSearchService,
        IPaymentPlanService planService,
        ISubscriptionBuilder subscriptionBuilder)
        : Controller
    {
        /// <summary>
        /// Search subscriptions by given criteria
        /// </summary>
        /// <param name="criteria">criteria</param>
        [HttpPost]
        [Route("search")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<SubscriptionSearchResult>> SearchSubscriptions([FromBody] SubscriptionSearchCriteria criteria)
        {
            var result = await subscriptionSearchService.SearchAsync(criteria);
            return Ok(result);
        }

        [HttpGet]
        [Route("{id}")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<Subscription>> GetSubscriptionById(string id, [FromQuery] string respGroup = null)
        {
            var retVal = (await subscriptionService.GetByIdAsync(id, respGroup));
            if (retVal != null)
            {
                retVal = (await subscriptionBuilder.TakeSubscription(retVal).ActualizeAsync()).Subscription;
            }
            return Ok(retVal);
        }

        [HttpGet]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Read)]
        public async Task<ActionResult<Subscription[]>> GetSubscriptionByIds([FromQuery] string[] ids, [FromQuery] string respGroup = null)
        {
            var retVal = await subscriptionService.GetAsync(ids, respGroup);
            foreach (var subscription in retVal)
            {
                await subscriptionBuilder.TakeSubscription(subscription).ActualizeAsync();
            }
            return Ok(retVal);
        }

        [HttpPost]
        [Route("order")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<ActionResult<CustomerOrder>> CreateRecurrentOrderForSubscription([FromBody] Subscription subscription)
        {
            var order = await subscriptionService.CreateOrderForSubscription(subscription);
            return Ok(order);
        }

        [HttpPost]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Create)]
        public async Task<ActionResult<Subscription>> CreateSubscription([FromBody] Subscription subscription)
        {
            await subscriptionBuilder.TakeSubscription(subscription).ActualizeAsync();
            await subscriptionService.SaveChangesAsync([subscription]);
            return Ok(subscription);
        }

        [HttpPost]
        [Route("cancel")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<ActionResult<Subscription>> CancelSubscription([FromBody] SubscriptionCancelRequest cancelRequest)
        {
            var retVal = (await subscriptionService.GetByIdAsync(cancelRequest.SubscriptionId));
            if (retVal != null)
            {
                await subscriptionBuilder.TakeSubscription(retVal).CancelSubscription(cancelRequest.CancelReason).ActualizeAsync();
                await subscriptionService.SaveChangesAsync([retVal]);
            }
            return Ok(retVal);
        }

        [HttpPut]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Update)]
        public async Task<ActionResult<Subscription>> UpdateSubscription([FromBody] Subscription subscription)
        {
            await subscriptionBuilder.TakeSubscription(subscription).ActualizeAsync();
            await subscriptionService.SaveChangesAsync([subscription]);
            return Ok(subscription);
        }

        /// <summary>
        ///  Delete subscriptions
        /// </summary>
        /// <param name="ids">subscriptions' ids for delete</param>
        [HttpDelete]
        [Route("")]
        [Authorize(ModuleConstants.Security.Permissions.Delete)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteSubscriptionsByIds([FromQuery] string[] ids)
        {
            await subscriptionService.DeleteAsync(ids);
            return NoContent();
        }

        [HttpGet]
        [Route("plans/{id}")]
        public async Task<ActionResult<PaymentPlan>> GetPaymentPlanById(string id)
        {
            var retVal = await planService.GetByIdAsync(id);
            return Ok(retVal);
        }

        [HttpGet]
        [Route("plans")]
        public async Task<ActionResult<PaymentPlan[]>> GetPaymentPlanByIds([FromQuery] string[] ids)
        {
            var retVal = await planService.GetAsync(ids);
            return Ok(retVal);
        }

        /// <summary>
        /// Gets plans by plenty ids 
        /// </summary>
        /// <param name="ids">Item ids</param>
        /// <returns></returns>
        [HttpPost]
        [Route("plans/plenty")]
        public async Task<ActionResult<PaymentPlan[]>> GetPaymentPlansByPlentyIds([FromBody] string[] ids)
        {
            var retVal = await planService.GetAsync(ids);
            return Ok(retVal);
        }

        [HttpPost]
        [Route("plans")]
        [Authorize(ModuleConstants.Security.Permissions.PlanManage)]
        public async Task<ActionResult<PaymentPlan>> CreatePaymentPlan([FromBody] PaymentPlan plan)
        {
            await planService.SaveChangesAsync([plan]);
            return Ok(plan);
        }

        [HttpPut]
        [Route("plans")]
        [Authorize(ModuleConstants.Security.Permissions.PlanManage)]
        public async Task<ActionResult<PaymentPlan>> UpdatePaymentPlan([FromBody] PaymentPlan plan)
        {
            await planService.SaveChangesAsync([plan]);
            return Ok(plan);
        }

        /// <summary>
        ///  Delete payment plans
        /// </summary>
        /// <param name="ids">plans' ids for delete</param>
        [HttpDelete]
        [Route("plans")]
        [Authorize(ModuleConstants.Security.Permissions.PlanManage)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeletePlansByIds([FromQuery] string[] ids)
        {
            await planService.DeleteAsync(ids);
            return NoContent();
        }
    }
}
