using System.Threading.Tasks;
using Hangfire;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.SubscriptionModule.Data.BackgroundJobs
{
    public class BackgroundJobsRunner
    {
        private readonly ISettingsManager _settingsManager;

        public BackgroundJobsRunner(ISettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        public async Task ConfigureProcessSubscriptionJob()
        {
            var processJobEnable = await _settingsManager.GetValueAsync(Core.ModuleConstants.Settings.General.EnableSubscriptionProcessJob.Name, true);
            if (processJobEnable)
            {
                var cronExpression = _settingsManager.GetValue(Core.ModuleConstants.Settings.General.CronExpression.Name, "0/5 * * * *");
                RecurringJob.AddOrUpdate<ProcessSubscriptionJob>("ProcessSubscriptionJob", x => x.Process(), cronExpression);
            }
            else
            {
                RecurringJob.RemoveIfExists("ProcessSubscriptionJob");
            }
        }

        public async Task ConfigureProcessSubscriptionOrdersJob()
        {
            var createOrderJobEnable = await _settingsManager.GetValueAsync(Core.ModuleConstants.Settings.General.EnableSubscriptionOrdersCreateJob.Name, true);
            if (createOrderJobEnable)
            {
                var cronExpressionOrder = _settingsManager.GetValue(Core.ModuleConstants.Settings.General.CronExpressionOrdersJob.Name, "0/15 * * * *");
                RecurringJob.AddOrUpdate<CreateRecurrentOrdersJob>("ProcessSubscriptionOrdersJob", x => x.Process(), cronExpressionOrder);
            }
            else
            {
                RecurringJob.RemoveIfExists("ProcessSubscriptionOrdersJob");
            }
        }
    }
}
