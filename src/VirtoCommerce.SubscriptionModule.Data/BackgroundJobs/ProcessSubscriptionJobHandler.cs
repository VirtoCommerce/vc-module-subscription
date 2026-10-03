using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Platform.Core.Jobs;

namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    /// <summary>
    /// Runs <see cref="ProcessSubscriptionJob"/> on the recurring schedule, one run at a time across the worker fleet.
    /// </summary>
    public class ProcessSubscriptionJobHandler(ProcessSubscriptionJob job, IDistributedLock distributedLock, ILogger<ProcessSubscriptionJobHandler> logger)
        : IBackgroundJobHandler<ProcessSubscriptionJobPayload>
    {
        // Replaces Hangfire's [DisableConcurrentExecution(10)] on the job class: wait up to 10 seconds for a previous run
        // to finish, then skip this occurrence; the next occurrence picks up the work. Unlike the Hangfire attribute, the
        // lock spans the whole worker fleet, not one Hangfire server.
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
}
