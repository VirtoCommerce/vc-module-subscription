using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Customer.Services;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Notifications;
using VirtoCommerce.SubscriptionModule.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Data.Notifications;

namespace VirtoCommerce.SubscriptionModule.Data.Observers
{
    public class SubscriptionNotificationObserver : IObserver<SubscriptionChangeEvent>
    {
        private readonly INotificationManager _notificationManager;
        private readonly IStoreService _storeService;
        private readonly IMemberService _memberService;

        public SubscriptionNotificationObserver(INotificationManager notificationManager, IStoreService storeService, IMemberService memberService)
        {
            _notificationManager = notificationManager;
            _storeService = storeService;
            _memberService = memberService;
        }

        #region IObserver<SubscriptionChangeEvent> Members
        public void OnCompleted()
        {            
        }

        public void OnError(Exception error)
        {           
        }

        public void OnNext(SubscriptionChangeEvent changeEvent)
        {
            //Collection of order notifications
            var notifications = new List<SubscriptionEmailNotificationBase>();

            if (IsSubscriptionCanceled(changeEvent))
            {
                //Resolve SubscriptionCanceledEmailNotification with template defined on store level
                var notification = _notificationManager.GetNewNotification<SubscriptionCanceledEmailNotification>(changeEvent.ModifiedSubscription.StoreId, "Store", changeEvent.ModifiedSubscription.CustomerOrderPrototype.LanguageCode);
                notifications.Add(notification);
            }

            if (changeEvent.ChangeState == EntryState.Added)
            {
                //Resolve NewSubscriptionEmailNotification with template defined on store level
                var notification = _notificationManager.GetNewNotification<NewSubscriptionEmailNotification>(changeEvent.ModifiedSubscription.StoreId, "Store", changeEvent.ModifiedSubscription.CustomerOrderPrototype.LanguageCode);
                notifications.Add(notification);
            }
        
            foreach (var notification in notifications)
            {
                SetNotificationParameters(notification, changeEvent);
                _notificationManager.ScheduleSendNotification(notification);
            }
        }
        #endregion

        /// <summary>
        /// Set base notification parameters (sender, recipient, isActive)
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="changeEvent"></param>
        private void SetNotificationParameters(SubscriptionEmailNotificationBase notification, SubscriptionChangeEvent changeEvent)
        {
            var store = _storeService.GetById(changeEvent.ModifiedSubscription.StoreId);

            notification.Subscription = changeEvent.ModifiedSubscription;
            notification.Sender = store.Email;
            notification.IsActive = true;
            //Link notification to subscription to getting notification history for each subscription individually
            notification.ObjectId = changeEvent.ModifiedSubscription.Id;
            notification.ObjectTypeId = typeof(Subscription).Name;

            var member = _memberService.GetByIds(new[] { changeEvent.ModifiedSubscription.CustomerId }).FirstOrDefault();
            if (member != null)
            {
                var email = member.Emails.FirstOrDefault();
                if (!string.IsNullOrEmpty(email))
                {
                    notification.Recipient = email;
                }
            }
            if (string.IsNullOrEmpty(notification.Recipient))
            {
                if (changeEvent.ModifiedSubscription.CustomerOrderPrototype.Addresses.Count > 0)
                {
                    var address = changeEvent.ModifiedSubscription.CustomerOrderPrototype.Addresses.FirstOrDefault();
                    if (address != null)
                    {
                        notification.Recipient = address.Email;
                    }
                }
            }
        }

        private bool IsSubscriptionCanceled(SubscriptionChangeEvent value)
        {
            var retVal = false;

            retVal = value.OriginalSubscription != null &&
                     value.OriginalSubscription.IsCancelled != value.ModifiedSubscription.IsCancelled &&
                     value.ModifiedSubscription.IsCancelled;

            return retVal;
        }

     

    }
}
