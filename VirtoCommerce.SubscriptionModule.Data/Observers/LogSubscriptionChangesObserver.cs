using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Order.Events;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.SubscriptionModule.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Data.Resources;

namespace VirtoCommerce.SubscriptionModule.Data.Observers
{
    /// <summary>
    /// Write human readable change log for any subscription change
    /// </summary>
    public class LogSubscriptionChangesObserver : IObserver<SubscriptionChangeEvent>, IObserver<OrderChangeEvent>
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
                    operationLogs.Add(GetLogRecord(modified.Id, SubscriptionResources.StatusChanged, original.SubscriptionStatus, modified.SubscriptionStatus));
                }
                if (original.Interval != modified.Interval)
                {
                    operationLogs.Add(GetLogRecord(modified.Id, SubscriptionResources.IntervalChanged, original.Interval, modified.Interval));
                }
                if (original.IntervalCount != modified.IntervalCount)
                {
                    operationLogs.Add(GetLogRecord(modified.Id, SubscriptionResources.IntervalCountChanged, original.IntervalCount, modified.IntervalCount));
                }
                if (original.TrialPeriodDays != modified.TrialPeriodDays)
                {
                    operationLogs.Add(GetLogRecord(modified.Id, SubscriptionResources.TrialPeriodChanged, original.TrialPeriodDays, modified.TrialPeriodDays));
                }
                if (original.CurrentPeriodEnd != modified.CurrentPeriodEnd)
                {
                    operationLogs.Add(GetLogRecord(modified.Id, SubscriptionResources.NextBillingDateChanged, original.CurrentPeriodEnd, modified.CurrentPeriodEnd));
                }
                if (original.Balance != modified.Balance)
                {
                    operationLogs.Add(GetLogRecord(modified.Id, SubscriptionResources.BalanceChanged, original.Balance, modified.Balance));
                }
                if (modified.IsCancelled && original.IsCancelled != modified.IsCancelled)
                {
                    operationLogs.Add(GetLogRecord(modified.Id, SubscriptionResources.SubscriptionCanceled, modified.CancelledDate, modified.CancelReason ?? ""));
                }
                if (original.OuterId != modified.OuterId)
                {
                    operationLogs.Add(GetLogRecord(modified.Id, SubscriptionResources.OuterIdChanged, original.OuterId, modified.OuterId));
                }

                _changeLogService.SaveChanges(operationLogs.ToArray());
            }
        }
        #endregion

        #region IObserver<OrderChangeEvent> Members
        public void OnNext(OrderChangeEvent value)
        {
            //log for new recurring orders creation
            //handle only recurring orders
            if (value.ChangeState == Platform.Core.Common.EntryState.Added && !string.IsNullOrEmpty(value.OrigOrder.SubscriptionId))
            {
                var operationLog = GetLogRecord(value.ModifiedOrder.SubscriptionId, SubscriptionResources.NewRecurringOrderCreated, value.ModifiedOrder.Number);
                _changeLogService.SaveChanges(new[] { operationLog } );
            }
        } 
        #endregion

        private static OperationLog GetLogRecord(string subscriptionId, string template, params object[] parameters)
        {
            var result = new OperationLog
            {
                ObjectId = subscriptionId,
                ObjectType = typeof(Subscription).Name,
                OperationType = Platform.Core.Common.EntryState.Modified,
                Detail = string.Format(template, parameters)
            };
            return result;
            
        }
    }
}
