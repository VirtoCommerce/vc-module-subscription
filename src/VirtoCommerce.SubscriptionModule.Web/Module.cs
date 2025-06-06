using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.TemplateLoader.FileSystem;
using VirtoCommerce.OrdersModule.Core.Events;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Extensions;
using VirtoCommerce.Platform.Hangfire;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core;
using VirtoCommerce.SubscriptionModule.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Data.BackgroundJobs;
using VirtoCommerce.SubscriptionModule.Data.ExportImport;
using VirtoCommerce.SubscriptionModule.Data.Handlers;
using VirtoCommerce.SubscriptionModule.Data.MySql;
using VirtoCommerce.SubscriptionModule.Data.Notifications;
using VirtoCommerce.SubscriptionModule.Data.PostgreSql;
using VirtoCommerce.SubscriptionModule.Data.Repositories;
using VirtoCommerce.SubscriptionModule.Data.Services;
using VirtoCommerce.SubscriptionModule.Data.SqlServer;

namespace VirtoCommerce.SubscriptionModule.Web
{
    public class Module : IModule, IExportSupport, IImportSupport, IHasConfiguration
    {
        private IApplicationBuilder _applicationBuilder;

        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<SubscriptionDbContext>(options =>
            {
                var databaseProvider = Configuration.GetValue("DatabaseProvider", "SqlServer");
                var connectionString = Configuration.GetConnectionString(ModuleInfo.Id) ?? Configuration.GetConnectionString("VirtoCommerce");

                switch (databaseProvider)
                {
                    case "MySql":
                        options.UseMySqlDatabase(connectionString);
                        break;
                    case "PostgreSql":
                        options.UsePostgreSqlDatabase(connectionString);
                        break;
                    default:
                        options.UseSqlServerDatabase(connectionString);
                        break;
                }
            });

            serviceCollection.AddTransient<ISubscriptionRepository, SubscriptionRepositoryImpl>();
            serviceCollection.AddSingleton<Func<ISubscriptionRepository>>(provider => () => provider.CreateScope().ServiceProvider.GetRequiredService<ISubscriptionRepository>());

            serviceCollection.AddTransient<ISubscriptionService, SubscriptionService>();
            serviceCollection.AddTransient<ISubscriptionSearchService, SubscriptionSearchService>();
            serviceCollection.AddTransient<IPaymentPlanService, PaymentPlanService>();
            serviceCollection.AddTransient<IPaymentPlanSearchService, PaymentPlanSearchService>();
            serviceCollection.AddTransient<ISubscriptionBuilder, SubscriptionBuilder>();

            serviceCollection.AddSingleton<CreateSubscriptionOrderChangedEventHandler>();
            serviceCollection.AddSingleton<LogChangesSubscriptionChangedEventHandler>();
            serviceCollection.AddSingleton<SendNotificationsSubscriptionChangedEventHandler>();

            serviceCollection.AddSingleton<SubscriptionExportImport>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            _applicationBuilder = appBuilder;

            // Register module permissions
            var permissionsRegistrar = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Subscription", ModuleConstants.Security.Permissions.AllPermissions);

            //Register setting in the store level
            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);
            settingsRegistrar.RegisterSettingsForType(ModuleConstants.Settings.StoreLevelSettings, nameof(Store));

            //Registration welcome email notification.
            appBuilder.RegisterEventHandler<OrderChangedEvent, CreateSubscriptionOrderChangedEventHandler>();
            appBuilder.RegisterEventHandler<OrderChangedEvent, LogChangesSubscriptionChangedEventHandler>();
            appBuilder.RegisterEventHandler<SubscriptionChangedEvent, LogChangesSubscriptionChangedEventHandler>();
            appBuilder.RegisterEventHandler<SubscriptionChangedEvent, SendNotificationsSubscriptionChangedEventHandler>();

            //Schedule periodic subscription processing job
            var recurringJobService = appBuilder.ApplicationServices.GetService<IRecurringJobService>();

            recurringJobService.WatchJobSetting(
                new SettingCronJobBuilder()
                    .SetEnablerSetting(ModuleConstants.Settings.General.EnableSubscriptionProcessJob)
                    .SetCronSetting(ModuleConstants.Settings.General.CronExpression)
                    .SetJobId("ProcessSubscriptionJob")
                    .ToJob<ProcessSubscriptionJob>(x => x.Process())
                    .Build());

            recurringJobService.WatchJobSetting(
            new SettingCronJobBuilder()
                .SetEnablerSetting(ModuleConstants.Settings.General.EnableSubscriptionOrdersCreateJob)
                .SetCronSetting(ModuleConstants.Settings.General.CronExpressionOrdersJob)
                .SetJobId("ProcessSubscriptionOrdersJob")
                .ToJob<CreateRecurrentOrdersJob>(x => x.Process())
                .Build());

            var notificationRegistrar = appBuilder.ApplicationServices.GetService<INotificationRegistrar>();
            var defaultTemplatesDirectory = Path.Combine(ModuleInfo.FullPhysicalPath, "NotificationTemplates");
            notificationRegistrar.RegisterNotification<NewSubscriptionEmailNotification>().WithTemplatesFromPath(defaultTemplatesDirectory);
            notificationRegistrar.RegisterNotification<SubscriptionCanceledEmailNotification>().WithTemplatesFromPath(defaultTemplatesDirectory);

            using var serviceScope = appBuilder.ApplicationServices.CreateScope();
            var databaseProvider = Configuration.GetValue("DatabaseProvider", "SqlServer");
            var subscriptionDbContext = serviceScope.ServiceProvider.GetRequiredService<SubscriptionDbContext>();
            if (databaseProvider == "SqlServer")
            {
                subscriptionDbContext.Database.MigrateIfNotApplied(MigrationName.GetUpdateV2MigrationName(ModuleInfo.Id));
            }
            subscriptionDbContext.Database.Migrate();
        }

        public void Uninstall()
        {
            // Method intentionally left empty.
        }

        public Task ExportAsync(Stream outStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
        {
            return _applicationBuilder.ApplicationServices.GetRequiredService<SubscriptionExportImport>().DoExportAsync(outStream, progressCallback, cancellationToken);
        }

        public Task ImportAsync(Stream inputStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
        {
            return _applicationBuilder.ApplicationServices.GetRequiredService<SubscriptionExportImport>().DoImportAsync(inputStream, progressCallback, cancellationToken);
        }
    }
}
