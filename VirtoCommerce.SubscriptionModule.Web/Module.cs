using System;
using System.Linq;
using System.Web.Http;
using Hangfire;
using Microsoft.Practices.Unity;
using VirtoCommerce.Domain.Order.Events;
using VirtoCommerce.Platform.Core.Bus;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Notifications;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;
using VirtoCommerce.SubscriptionModule.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Data.Handlers;
using VirtoCommerce.SubscriptionModule.Data.Migrations;
using VirtoCommerce.SubscriptionModule.Data.Notifications;
using VirtoCommerce.SubscriptionModule.Data.Repositories;
using VirtoCommerce.SubscriptionModule.Data.Resources;
using VirtoCommerce.SubscriptionModule.Data.Services;
using VirtoCommerce.SubscriptionModule.Web.BackgroundJobs;
using VirtoCommerce.SubscriptionModule.Web.ExportImport;
using VirtoCommerce.SubscriptionModule.Web.JsonConverters;

namespace VirtoCommerce.SubscriptionModule.Web
{
    public class Module : ModuleBase, ISupportExportImportModule
    {
        private const string _connectionStringName = "VirtoCommerce";
        private readonly IUnityContainer _container;

   

        public Module(IUnityContainer container)
        {
            _container = container;
        }

        #region IModule Members

        public override void SetupDatabase()
        {
            using (var context = new SubscriptionRepositoryImpl(_connectionStringName, _container.Resolve<AuditableInterceptor>()))
            {
                var initializer = new SetupDatabaseInitializer<SubscriptionRepositoryImpl, Configuration>();
                initializer.InitializeDatabase(context);
            }
        }

        public override void Initialize()
        {
            base.Initialize();


            var eventHandlerRegistrar = _container.Resolve<IHandlerRegistrar>();

            //Registration welcome email notification.
            eventHandlerRegistrar.RegisterHandler<OrderChangedEvent>(async (message, token) => await _container.Resolve<OrderChangedEventHandler>().Handle(message));
            eventHandlerRegistrar.RegisterHandler<OrderChangedEvent>(async (message, token) => await _container.Resolve<LogChangesSubscriptionChangedEventHandler>().Handle(message));
            eventHandlerRegistrar.RegisterHandler<SubscriptionChangedEvent>(async (message, token) => await _container.Resolve<LogChangesSubscriptionChangedEventHandler>().Handle(message));
            eventHandlerRegistrar.RegisterHandler<SubscriptionChangedEvent>(async (message, token) => await _container.Resolve<SendNotificationsSubscriptionChangedEventHandler>().Handle(message));

            _container.RegisterType<ISubscriptionRepository>(new InjectionFactory(c => new SubscriptionRepositoryImpl(_connectionStringName, _container.Resolve<AuditableInterceptor>(), new EntityPrimaryKeyGeneratorInterceptor())));
         
            _container.RegisterType<ISubscriptionService, SubscriptionServiceImpl>();
            _container.RegisterType<ISubscriptionSearchService, SubscriptionServiceImpl>();
            _container.RegisterType<IPaymentPlanService, PaymentPlanServiceImpl>();
            _container.RegisterType<IPaymentPlanSearchService, PaymentPlanServiceImpl>();
            _container.RegisterType<ISubscriptionBuilder, SubscriptionBuilderImpl>();
        }

        public override void PostInitialize()
        {
            base.PostInitialize();

            var settingsManager = _container.Resolve<ISettingsManager>();

            //Register setting in the store level
            var storeLevelSettings = new[] { "Subscription.EnableSubscriptions" };
            settingsManager.RegisterModuleSettings("VirtoCommerce.Store", settingsManager.GetModuleSettings(base.ModuleInfo.Id).Where(x => storeLevelSettings.Contains(x.Name)).ToArray());

            //Schedule periodic subscription processing job
            var processJobEnable = settingsManager.GetValue("Subscription.EnableSubscriptionProccessJob", true);
            if (processJobEnable)
            {
                var cronExpression = settingsManager.GetValue("Subscription.CronExpression", "0/5 * * * *");
                RecurringJob.AddOrUpdate<ProcessSubscriptionJob>("ProcessSubscriptionJob", x => x.Process(), cronExpression);
            }
            else
            {
                RecurringJob.RemoveIfExists("ProcessSubscriptionJob");
            }

            var createOrderJobEnable = settingsManager.GetValue("Subscription.EnableSubscriptionOrdersCreatejob", true);
            if (createOrderJobEnable)
            {
                var cronExpressionOrder = settingsManager.GetValue("Subscription.CronExpressionOrdersJob", "0/15 * * * *");
                RecurringJob.AddOrUpdate<CreateRecurrentOrdersJob>("ProcessSubscriptionOrdersJob", x => x.Process(), cronExpressionOrder);
            }
            else
            {
                RecurringJob.RemoveIfExists("ProcessSubscriptionOrdersJob");
            }

            var notificationManager = _container.Resolve<INotificationManager>();
            notificationManager.RegisterNotificationType(() => new NewSubscriptionEmailNotification(_container.Resolve<IEmailNotificationSendingGateway>())
            {
                DisplayName = "New subscription notification",
                Description = "This notification sends by email to client when created new subscription",
                NotificationTemplate = new NotificationTemplate
                {
                    Body = SubscriptionResources.NewSubscriptionEmailNotificationBody,
                    Subject = SubscriptionResources.NewSubscriptionEmailNotificationSubject,
                    Language = "en-US"
                }
            });

            notificationManager.RegisterNotificationType(() => new SubscriptionCanceledEmailNotification(_container.Resolve<IEmailNotificationSendingGateway>())
            {
                DisplayName = "Subscription canceled notification",
                Description = "This notification sends by email to client when subscription was canceled",
                NotificationTemplate = new NotificationTemplate
                {
                    Body = SubscriptionResources.SubscriptionCanceledEmailNotificationBody,
                    Subject = SubscriptionResources.SubscriptionCanceledEmailNotificationSubject,
                    Language = "en-US"
                }
            });


            //Next lines allow to use polymorph types in API controller methods
            var httpConfiguration = _container.Resolve<HttpConfiguration>();
            httpConfiguration.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new PolymorphicSubscriptionJsonConverter());

        }
        #endregion

        #region ISupportExportImportModule Members
        public void DoExport(System.IO.Stream outStream, PlatformExportManifest manifest, Action<ExportImportProgressInfo> progressCallback)
        {
            var job = _container.Resolve<SubscriptionExportImport>();
            job.DoExport(outStream, progressCallback);
        }

        public void DoImport(System.IO.Stream inputStream, PlatformExportManifest manifest, Action<ExportImportProgressInfo> progressCallback)
        {
            var job = _container.Resolve<SubscriptionExportImport>();
            job.DoImport(inputStream, progressCallback);
        }

        public string ExportDescription
        {
            get
            {
                var settingManager = _container.Resolve<ISettingsManager>();
                return settingManager.GetValue("Subscription.ExportImport.Description", String.Empty);
            }
        }
        #endregion


    }
}
