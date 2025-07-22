using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.SubscriptionModule.Core.Model;

namespace VirtoCommerce.SubscriptionModule.Core.Services
{
    public interface ISubscriptionService : IOuterEntityService<Subscription>
    {
        Task<CustomerOrder> CreateOrderForSubscription(Subscription subscription);
    }
}
