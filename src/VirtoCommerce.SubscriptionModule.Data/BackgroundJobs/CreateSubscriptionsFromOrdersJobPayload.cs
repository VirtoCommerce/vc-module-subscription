using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    /// <summary>
    /// Payload of <see cref="CreateSubscriptionsFromOrdersJob"/>: the ids of the newly placed orders to create subscriptions for.
    /// </summary>
    /// <remarks>
    /// Carries ids, not orders: the job serializer has no polymorphic converters, so an order with an abstract
    /// <c>PaymentMethod</c> or <c>ShippingMethod</c> could not be read back. The job loads the orders by id instead.
    /// </remarks>
    public class CreateSubscriptionsFromOrdersJobPayload
    {
        public string[] OrderIds { get; set; } = [];

        /// <summary>
        /// The orders of a payload queued by 3.1004.0-3.1006.0, which carried whole orders. Only the order id is read;
        /// all other order members are skipped. Never written to a new payload.
        /// </summary>
        [JsonProperty("Orders", NullValueHandling = NullValueHandling.Ignore)]
        private LegacyOrder[] LegacyOrders { get; set; }

        /// <summary>
        /// Moves the ids of <see cref="LegacyOrders"/> to <see cref="OrderIds"/>, so that a requeued job still runs.
        /// </summary>
        [OnDeserialized]
        private void MergeLegacyOrderIds(StreamingContext context)
        {
            if (LegacyOrders != null)
            {
                OrderIds = OrderIds.Concat(LegacyOrders.Select(x => x.Id)).ToArray();
                LegacyOrders = null;
            }
        }

        private sealed record LegacyOrder(string Id);
    }
}
