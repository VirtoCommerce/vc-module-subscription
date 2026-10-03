using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Platform.Core.Jobs;
using VirtoCommerce.SubscriptionModule.Data.Handlers;

namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    // The recurring handlers below replace Hangfire's [DisableConcurrentExecution(10)] on the job classes: wait up to
    // 10 seconds for a previous run to finish, then skip this occurrence; the next occurrence picks up the work. Two
    // overlapping order-creation passes could create a subscription's recurrent order twice. Unlike the Hangfire
    // attribute, the lock spans the whole worker fleet, not one Hangfire server.

    public class ProcessSubscriptionJobPayload
    {
    }

    /// <summary>
    /// Runs <see cref="ProcessSubscriptionJob"/> on the recurring schedule, one run at a time across the worker fleet.
    /// </summary>
    public class ProcessSubscriptionJobHandler(ProcessSubscriptionJob job, IDistributedLock distributedLock, ILogger<ProcessSubscriptionJobHandler> logger)
        : IBackgroundJobHandler<ProcessSubscriptionJobPayload>
    {
        private const string LockResource = "subscription:job:process-subscriptions";
        private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(10);

        public virtual async Task Execute(ProcessSubscriptionJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var ran = await distributedLock.TryExecuteAsync(LockResource, _ => job.Process(), _lockTimeout, cancellationToken);
            if (!ran)
            {
                logger.LogInformation("Skipped processing subscriptions: a previous run still holds the lock.");
            }
        }
    }

    public class CreateRecurrentOrdersJobPayload
    {
    }

    /// <summary>
    /// Runs <see cref="CreateRecurrentOrdersJob"/> on the recurring schedule, one run at a time across the worker fleet.
    /// </summary>
    public class CreateRecurrentOrdersJobHandler(CreateRecurrentOrdersJob job, IDistributedLock distributedLock, ILogger<CreateRecurrentOrdersJobHandler> logger)
        : IBackgroundJobHandler<CreateRecurrentOrdersJobPayload>
    {
        private const string LockResource = "subscription:job:create-recurrent-orders";
        private static readonly TimeSpan _lockTimeout = TimeSpan.FromSeconds(10);

        public virtual async Task Execute(CreateRecurrentOrdersJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var ran = await distributedLock.TryExecuteAsync(LockResource, _ => job.Process(), _lockTimeout, cancellationToken);
            if (!ran)
            {
                logger.LogInformation("Skipped creating recurrent orders: a previous run still holds the lock.");
            }
        }
    }

    public class CreateSubscriptionsFromOrdersJobPayload
    {
        public CustomerOrder[] Orders { get; set; } = [];
    }

    /// <summary>
    /// Creates subscriptions for newly placed orders, off the request thread. The logic stays on
    /// <see cref="CreateSubscriptionOrderChangedEventHandler.HandleOrderChangesInBackground"/>, which existing
    /// customizations may override.
    /// </summary>
    public class CreateSubscriptionsFromOrdersJob(CreateSubscriptionOrderChangedEventHandler orderChangedEventHandler)
        : IBackgroundJobHandler<CreateSubscriptionsFromOrdersJobPayload>
    {
        public virtual Task Execute(CreateSubscriptionsFromOrdersJobPayload payload, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            orderChangedEventHandler.HandleOrderChangesInBackground(payload.Orders);
            return Task.CompletedTask;
        }
    }
}
