using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Web.Model;

namespace VirtoCommerce.SubscriptionModule.Web.Controllers.Api
{
    [RoutePrefix("api/subscriptions")]
    public class SubscriptionModuleController : ApiController
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubscriptionSearchService _subscriptionSearchService;
        private readonly IPaymentPlanService _planService;
        private readonly ISubscriptionBuilder _subscriptionBuilder;
        private readonly ICustomerOrderService _customerOrderService;
        public SubscriptionModuleController(ISubscriptionService subscriptionService, ISubscriptionSearchService subscriptionSearchService, IPaymentPlanService planService, 
                                            ISubscriptionBuilder subscriptionBuilder, ICustomerOrderService customerOrderService)
        {
            _subscriptionService = subscriptionService;
            _subscriptionSearchService = subscriptionSearchService;
            _planService = planService;
            _subscriptionBuilder = subscriptionBuilder;
            _customerOrderService = customerOrderService;
        }

        /// <summary>
        /// Search subscriptions by given criteria
        /// </summary>
        /// <param name="criteria">criteria</param>
        [HttpPost]
        [Route("search")]
        [ResponseType(typeof(SubscriptionSearchResult))]
        public IHttpActionResult Search(SubscriptionSearchCriteria criteria)
        {
            var result = _subscriptionSearchService.SearchSubscriptions(criteria);
            var retVal = new SubscriptionSearchResult
            {
                Subscriptions = result.Results.ToList(),
                TotalCount = result.TotalCount
            };
            return Ok(retVal);
        }


        [HttpGet]
        [Route("{id}")]
        [ResponseType(typeof(Subscription))]
        public IHttpActionResult GetSubscriptionById(string id, [FromUri] string respGroup = null)
        {
            var retVal = _subscriptionService.GetByIds(new[] { id }, respGroup).FirstOrDefault();
            if(retVal != null)
            {
                retVal = _subscriptionBuilder.TakeSubscription(retVal).Actualize().Subscription;
            }
            return Ok(retVal);
        }

        [HttpGet]
        [Route("")]
        [ResponseType(typeof(Subscription[]))]
        public IHttpActionResult GetSubscriptionByIds([FromUri] string[] ids, [FromUri] string respGroup = null)
        {
            var retVal = _subscriptionService.GetByIds(ids, respGroup);
            foreach(var subscription in retVal)
            {
                _subscriptionBuilder.TakeSubscription(subscription).Actualize();
            }
            return Ok(retVal);
        }

        [HttpPost]
        [Route("order")]
        [ResponseType(typeof(CustomerOrder))]
        public IHttpActionResult CreateReccurentOrderForSubscription(Subscription subscription)
        {
            var order = _subscriptionBuilder.TakeSubscription(subscription).Actualize()
                                             .TryToCreateRecurrentOrder(forceCreation: true);
            _customerOrderService.SaveChanges(new[] { order });
            return Ok(order);
        }

        [HttpPost]
        [Route("")]
        [ResponseType(typeof(Subscription))]
        public IHttpActionResult CreateSubscription(Subscription subscription)
        {
            _subscriptionBuilder.TakeSubscription(subscription).Actualize();
            _subscriptionService.SaveSubscriptions(new[] { subscription });
            return Ok(subscription);
        }

        [HttpPost]
        [Route("cancel")]
        [ResponseType(typeof(Subscription))]
        public IHttpActionResult CancelSubscription(SubscriptionCancelRequest cancelRequest)
        {
            var retVal = _subscriptionService.GetByIds(new[] { cancelRequest.SubscriptionId }).FirstOrDefault();
            if (retVal != null)
            {
                _subscriptionBuilder.TakeSubscription(retVal).CancelSubscription(cancelRequest.CancelReason).Actualize();
                _subscriptionService.SaveSubscriptions(new[] { retVal });
            }
            return Ok(retVal);
        }

        [HttpPut]
        [Route("")]
        [ResponseType(typeof(Subscription))]
        public IHttpActionResult UpdateSubscription(Subscription subscription)
        {
            _subscriptionBuilder.TakeSubscription(subscription).Actualize();
            _subscriptionService.SaveSubscriptions(new[] { subscription });
            return Ok(subscription);
        }

        /// <summary>
        ///  Delete subscriptions
        /// </summary>
        /// <param name="ids">subscriptions' ids for delete</param>
        [HttpDelete]
        [Route("")]
        [ResponseType(typeof(void))]
        public IHttpActionResult DeleteSubscriptionsByIds([FromUri] string[] ids)
        {
            _subscriptionService.Delete(ids);
            return StatusCode(HttpStatusCode.NoContent);
        }


        [HttpGet]
        [Route("plans/{id}")]
        [ResponseType(typeof(PaymentPlan))]
        public IHttpActionResult GetPaymentPlanById(string id)
        {
            var retVal = _planService.GetByIds(new[] { id }).FirstOrDefault();
            return Ok(retVal);
        }

        [HttpPost]
        [Route("plans")]
        [ResponseType(typeof(PaymentPlan))]
        public IHttpActionResult CreatePaymentPlan(PaymentPlan plan)
        {
            _planService.SavePlans(new[] { plan });
            return Ok(plan);
        }

        [HttpPut]
        [Route("plans")]
        [ResponseType(typeof(PaymentPlan))]
        public IHttpActionResult UpdatePaymentPlan(PaymentPlan plan)
        {
            _planService.SavePlans(new[] { plan });
            return Ok(plan);
        }

        /// <summary>
        ///  Delete payment plans
        /// </summary>
        /// <param name="ids">plans' ids for delete</param>
        [HttpDelete]
        [Route("plans")]
        [ResponseType(typeof(void))]
        public IHttpActionResult DeletePlansByIds([FromUri] string[] ids)
        {
            _planService.Delete(ids);
            return StatusCode(HttpStatusCode.NoContent);
        }

    }
}