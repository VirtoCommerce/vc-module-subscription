using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Settings.Events;
using VirtoCommerce.SubscriptionModule.Data.BackgroundJobs;

namespace VirtoCommerce.SubscriptionModule.Data.Handlers
{
    public class ObjectSettingEntryChangedEventHandler : IEventHandler<ObjectSettingChangedEvent>
    {
        private readonly BackgroundJobsRunner _backgroundJobsRunner;

        public ObjectSettingEntryChangedEventHandler(BackgroundJobsRunner backgroundJobsRunner)
        {
            _backgroundJobsRunner = backgroundJobsRunner;
        }

        public virtual async Task Handle(ObjectSettingChangedEvent message)
        {
            if (message.ChangedEntries.Any(x => (x.EntryState == EntryState.Modified
                                              || x.EntryState == EntryState.Added)
                                  && (x.NewEntry.Name == Core.ModuleConstants.Settings.General.EnableSubscriptionProcessJob.Name
                                   || x.NewEntry.Name == Core.ModuleConstants.Settings.General.CronExpression.Name)))
            {
                await _backgroundJobsRunner.ConfigureProcessSubscriptionJob();
            }


            if (message.ChangedEntries.Any(x => (x.EntryState == EntryState.Modified
                                              || x.EntryState == EntryState.Added)
                                  && (x.NewEntry.Name == Core.ModuleConstants.Settings.General.EnableSubscriptionOrdersCreateJob.Name
                                   || x.NewEntry.Name == Core.ModuleConstants.Settings.General.CronExpressionOrdersJob.Name)))
            {
                await _backgroundJobsRunner.ConfigureProcessSubscriptionOrdersJob();
            }
        }
    }
}
