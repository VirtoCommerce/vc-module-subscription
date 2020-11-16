using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model;

namespace VirtoCommerce.SubscriptionModule.Core.Services
{
    public interface ISubscriptionService
    {
        Task<Subscription[]> GetByIdsAsync(string[] subscriptionIds, string responseGroup = null);
        Task SaveSubscriptionsAsync(Subscription[] subscriptions);
        Task DeleteAsync(string[] ids);
        Task<CustomerOrder> CreateOrderForSubscription(Subscription subscription);
    }
}
