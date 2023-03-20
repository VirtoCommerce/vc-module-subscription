using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.TemplateLoader.FileSystem;
using VirtoCommerce.OrdersModule.Core.Events;
using VirtoCommerce.Platform.Core.Bus;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Core.Settings.Events;
using VirtoCommerce.Platform.Data.Extensions;
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

        #region IModule Members

        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }


        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<SubscriptionDbContext>((provider, options) =>
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
            //Register as scoped because we use UserManager<> as dependency in this implementation
            serviceCollection.AddScoped<SendNotificationsSubscriptionChangedEventHandler>();

            serviceCollection.AddSingleton<SubscriptionExportImport>();

            serviceCollection.AddTransient<ObjectSettingEntryChangedEventHandler>();
            serviceCollection.AddTransient<BackgroundJobsRunner>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            _applicationBuilder = appBuilder;

            // Register module permissions
            var permissionsRegistrar = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            var permissions = ModuleConstants.Security.Permissions.AllPermissions.Select(permissionName => new Permission
            {
                ModuleId = ModuleInfo.Id,
                GroupName = "Subscription",
                Name = permissionName
            });
            permissionsRegistrar.RegisterPermissions(permissions.ToArray());

            //Register setting in the store level
            var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
            settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);
            settingsRegistrar.RegisterSettingsForType(ModuleConstants.Settings.StoreLevelSettings, typeof(Store).Name);

            //Registration welcome email notification.
            var handlerRegistrar = appBuilder.ApplicationServices.GetRequiredService<IHandlerRegistrar>();
            handlerRegistrar.RegisterHandler<OrderChangedEvent>(async (message, token) => await appBuilder.ApplicationServices.GetRequiredService<CreateSubscriptionOrderChangedEventHandler>().Handle(message));
            handlerRegistrar.RegisterHandler<OrderChangedEvent>(async (message, token) => await appBuilder.ApplicationServices.GetRequiredService<LogChangesSubscriptionChangedEventHandler>().Handle(message));
            handlerRegistrar.RegisterHandler<SubscriptionChangedEvent>(async (message, token) => await appBuilder.ApplicationServices.GetRequiredService<LogChangesSubscriptionChangedEventHandler>().Handle(message));
            handlerRegistrar.RegisterHandler<SubscriptionChangedEvent>(async (message, token) => await appBuilder.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<SendNotificationsSubscriptionChangedEventHandler>().Handle(message));

            //Subscribe for subscription processing job configuration changes
            var inProcessBus = appBuilder.ApplicationServices.GetService<IHandlerRegistrar>();
            inProcessBus.RegisterHandler<ObjectSettingChangedEvent>(async (message, token) => await appBuilder.ApplicationServices.GetService<ObjectSettingEntryChangedEventHandler>().Handle(message));

            //Schedule periodic subscription processing job
            var jobsRunner = appBuilder.ApplicationServices.GetService<BackgroundJobsRunner>();
            jobsRunner.ConfigureProcessSubscriptionJob().GetAwaiter().GetResult();
            jobsRunner.ConfigureProcessSubscriptionOrdersJob().GetAwaiter().GetResult();

            var notificationRegistrar = appBuilder.ApplicationServices.GetService<INotificationRegistrar>();
            var defaultTemplatesDirectory = Path.Combine(ModuleInfo.FullPhysicalPath, "NotificationTemplates");
            notificationRegistrar.RegisterNotification<NewSubscriptionEmailNotification>().WithTemplatesFromPath(defaultTemplatesDirectory);
            notificationRegistrar.RegisterNotification<SubscriptionCanceledEmailNotification>().WithTemplatesFromPath(defaultTemplatesDirectory);

            using (var serviceScope = appBuilder.ApplicationServices.CreateScope())
            {
                var databaseProvider = Configuration.GetValue("DatabaseProvider", "SqlServer");
                var subscriptionDbContext = serviceScope.ServiceProvider.GetRequiredService<SubscriptionDbContext>();
                if (databaseProvider == "SqlServer")
                {
                    subscriptionDbContext.Database.MigrateIfNotApplied(MigrationName.GetUpdateV2MigrationName(ModuleInfo.Id));
                }
                subscriptionDbContext.Database.Migrate();
            }
        }

        public void Uninstall()
        {
            // Method intentionally left empty.
        }

        #endregion

        public async Task ExportAsync(Stream outStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback,
            ICancellationToken cancellationToken)
        {
            await _applicationBuilder.ApplicationServices.GetRequiredService<SubscriptionExportImport>().DoExportAsync(outStream,
                progressCallback, cancellationToken);
        }

        public async Task ImportAsync(Stream inputStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback,
            ICancellationToken cancellationToken)
        {
            await _applicationBuilder.ApplicationServices.GetRequiredService<SubscriptionExportImport>().DoImportAsync(inputStream,
                progressCallback, cancellationToken);
        }
    }
}
