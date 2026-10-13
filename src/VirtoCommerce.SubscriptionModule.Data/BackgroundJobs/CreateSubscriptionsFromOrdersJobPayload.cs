using System.Linq;
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
        /// Reads the order ids from a payload queued by 3.1004.0-3.1006.0, which carried whole orders, so that a
        /// requeued job still runs. Only the order id is read; all other order members are skipped.
        /// </summary>
        [JsonProperty("Orders")]
        private LegacyOrder[] LegacyOrders
        {
            set => OrderIds = OrderIds.Concat(value?.Select(x => x.Id) ?? []).ToArray();
        }

        private sealed class LegacyOrder
        {
            public string Id { get; set; }
        }
    }
}
