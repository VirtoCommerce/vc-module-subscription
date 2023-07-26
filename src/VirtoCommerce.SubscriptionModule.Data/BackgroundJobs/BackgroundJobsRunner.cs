using System.Threading.Tasks;
using Hangfire;
using VirtoCommerce.Platform.Core.Settings;
using SubscriptionSettings = VirtoCommerce.SubscriptionModule.Core.ModuleConstants.Settings.General;

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
            var processJobEnable = await _settingsManager.GetValueAsync<bool>(SubscriptionSettings.EnableSubscriptionProcessJob);
            if (processJobEnable)
            {
                var cronExpression = await _settingsManager.GetValueAsync<string>(SubscriptionSettings.CronExpression);
                RecurringJob.AddOrUpdate<ProcessSubscriptionJob>("ProcessSubscriptionJob", x => x.Process(), cronExpression);
            }
            else
            {
                RecurringJob.RemoveIfExists("ProcessSubscriptionJob");
            }
        }

        public async Task ConfigureProcessSubscriptionOrdersJob()
        {
            var createOrderJobEnable = await _settingsManager.GetValueAsync<bool>(SubscriptionSettings.EnableSubscriptionOrdersCreateJob);
            if (createOrderJobEnable)
            {
                var cronExpressionOrder = await _settingsManager.GetValueAsync<string>(SubscriptionSettings.CronExpressionOrdersJob);
                RecurringJob.AddOrUpdate<CreateRecurrentOrdersJob>("ProcessSubscriptionOrdersJob", x => x.Process(), cronExpressionOrder);
            }
            else
            {
                RecurringJob.RemoveIfExists("ProcessSubscriptionOrdersJob");
            }
        }
    }
}
