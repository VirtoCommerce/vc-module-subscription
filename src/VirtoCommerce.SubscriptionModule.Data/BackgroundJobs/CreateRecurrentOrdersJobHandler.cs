using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.Platform.Core.DistributedLock;
using VirtoCommerce.Platform.Core.Jobs;

namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    /// <summary>
    /// Runs <see cref="CreateRecurrentOrdersJob"/> on the recurring schedule, one run at a time across the worker fleet.
    /// </summary>
    public class CreateRecurrentOrdersJobHandler(CreateRecurrentOrdersJob job, IDistributedLock distributedLock, ILogger<CreateRecurrentOrdersJobHandler> logger)
        : IBackgroundJobHandler<CreateRecurrentOrdersJobPayload>
    {
        // Replaces Hangfire's [DisableConcurrentExecution(10)] on the job class: wait up to 10 seconds for a previous run
        // to finish, then skip this occurrence; the next occurrence picks up the work. Two overlapping order-creation
        // passes could create a subscription's recurrent order twice. Unlike the Hangfire attribute, the lock spans the
        // whole worker fleet, not one Hangfire server.
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
}
