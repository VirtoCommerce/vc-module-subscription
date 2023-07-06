using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model;

namespace VirtoCommerce.SubscriptionModule.Data.Notifications
{
    public class SubscriptionEmailNotificationBase : EmailNotification
    {
        public SubscriptionEmailNotificationBase(string type)
            : base(type)
        {
        }

        public Subscription Subscription { get; set; }
    }
}
