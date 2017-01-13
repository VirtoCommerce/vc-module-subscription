using System;
using Hangfire;
using Microsoft.Practices.Unity;
using VirtoCommerce.SubscriptionModule.Core.Services;
using VirtoCommerce.SubscriptionModule.Data.Migrations;
using VirtoCommerce.SubscriptionModule.Data.Observers;
using VirtoCommerce.SubscriptionModule.Data.Repositories;
using VirtoCommerce.SubscriptionModule.Data.Services;
using VirtoCommerce.SubscriptionModule.Web.BackgroundJobs;
using VirtoCommerce.Domain.Order.Events;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.SubscriptionModule.Core.Events;
using VirtoCommerce.Platform.Core.Notifications;
using VirtoCommerce.SubscriptionModule.Data.Resources;
using VirtoCommerce.SubscriptionModule.Data.Notifications;
using System.Linq;
using VirtoCommerce.Platform.Core.ExportImport;
using System.IO;
using VirtoCommerce.SubscriptionModule.Web.ExportImport;
using System.Web.Http;
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

            _container.RegisterType<IEventPublisher<SubscriptionChangeEvent>, EventPublisher<SubscriptionChangeEvent>>();
            //Log subscription request changes
            _container.RegisterType<IObserver<SubscriptionChangeEvent>, LogSubscriptionChangesObserver>("LogSubscriptionChangesObserver");
            _container.RegisterType<IObserver<SubscriptionChangeEvent>, SubscriptionNotificationObserver>("SubscriptionNotificationObserver");
           
            _container.RegisterType<ISubscriptionRepository>(new InjectionFactory(c => new SubscriptionRepositoryImpl(_connectionStringName, _container.Resolve<AuditableInterceptor>(), new EntityPrimaryKeyGeneratorInterceptor())));
         
            _container.RegisterType<ISubscriptionService, SubscriptionServiceImpl>();
            _container.RegisterType<ISubscriptionSearchService, SubscriptionServiceImpl>();
            _container.RegisterType<IPaymentPlanService, PaymentPlanServiceImpl>();
            _container.RegisterType<IPaymentPlanSearchService, PaymentPlanServiceImpl>();
            _container.RegisterType<ISubscriptionBuilder, SubscriptionBuilderImpl>();
            
            //Subscribe to the order change event. Try to create subscription for each new order

            //This registration with constructor parameters necessary because without it Unity raise stack overflow exception            
            _container.RegisterType<IObserver<OrderChangeEvent>, OrderSubscriptionObserver>("CreateSubscriptionObserver", new InjectionConstructor(new ResolvedParameter<ISubscriptionBuilder>(), _container.Resolve<ISubscriptionService>()));
            _container.RegisterType<IObserver<OrderChangeEvent>, LogSubscriptionChangesObserver>("LogSubscriptionChangesObserver");
        }

        public override void PostInitialize()
        {
            base.PostInitialize();

            var settingsManager = _container.Resolve<ISettingsManager>();

            //Register setting in the store level
            var storeLevelSettings = new[] { "Subscription.EnableSubscriptions" };
            settingsManager.RegisterModuleSettings("VirtoCommerce.Store", settingsManager.GetModuleSettings(base.ModuleInfo.Id).Where(x => storeLevelSettings.Contains(x.Name)).ToArray());

            //Schedule periodic subscription processing job
            var cronExpression = settingsManager.GetValue("Subscription.CronExpression", "0/5 * * * *");
            RecurringJob.AddOrUpdate<ProcessSubscriptionJob>("ProcessSubscriptionJob", x => x.Process(), cronExpression);

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
