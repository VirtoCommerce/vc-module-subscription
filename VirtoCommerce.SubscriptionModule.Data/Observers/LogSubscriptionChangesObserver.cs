using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.SubscriptionModule.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Data.Resources;

namespace VirtoCommerce.SubscriptionModule.Data.Observers
{
    public class LogSubscriptionChangesObserver : IObserver<SubscriptionChangeEvent>
    {
        private readonly IChangeLogService _changeLogService;
        public LogSubscriptionChangesObserver(IChangeLogService changeLogService)
        {
            _changeLogService = changeLogService;
        }
   
        #region IObserver<SubscriptionChangeEvent> Members
        public void OnCompleted()
        {
         
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(SubscriptionChangeEvent value)
        {
            var original = value.OriginalSubscription;
            var modified = value.ModifiedSubscription;

            if (value.ChangeState == Platform.Core.Common.EntryState.Modified)
            {
                var operationLogs = new List<OperationLog>();

                if (original.SubscriptionStatus != modified.SubscriptionStatus)
                {
                    operationLogs.Add(GetLogRecord(modified, SubscriptionResources.StatusChanged, original.SubscriptionStatus, modified.SubscriptionStatus));
                }
                if (original.Interval != modified.Interval)
                {
                    operationLogs.Add(GetLogRecord(modified, SubscriptionResources.IntervalChanged, original.Interval, modified.Interval));
                }
                if (original.IntervalCount != modified.IntervalCount)
                {
                    operationLogs.Add(GetLogRecord(modified, SubscriptionResources.IntervalCountChanged, original.IntervalCount, modified.IntervalCount));
                }
                if (original.TrialPeriodDays != modified.TrialPeriodDays)
                {
                    operationLogs.Add(GetLogRecord(modified, SubscriptionResources.TrialPeriodChanged, original.TrialPeriodDays, modified.TrialPeriodDays));
                }
                if (original.CurrentPeriodEnd != modified.CurrentPeriodEnd)
                {
                    operationLogs.Add(GetLogRecord(modified, SubscriptionResources.NextBillingDateChanged, original.CurrentPeriodEnd, modified.CurrentPeriodEnd));
                }
                if (original.Balance != modified.Balance)
                {
                    operationLogs.Add(GetLogRecord(modified, SubscriptionResources.BalanceChanged, original.Balance, modified.Balance));
                }
                if (modified.IsCancelled && original.IsCancelled != modified.IsCancelled)
                {
                    operationLogs.Add(GetLogRecord(modified, SubscriptionResources.SubscriptionCanceled, modified.CancelledDate, modified.CancelReason ?? ""));
                }

                _changeLogService.SaveChanges(operationLogs.ToArray());
            }
        } 
        #endregion

        private static OperationLog GetLogRecord(Subscription subscription, string template, params object[] parameters)
        {
            var result = new OperationLog
            {
                ObjectId = subscription.Id,
                ObjectType = typeof(Subscription).Name,
                OperationType = Platform.Core.Common.EntryState.Modified,
                Detail = string.Format(template, parameters)
            };
            return result;
            
        }
    }
}
