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

namespace VirtoCommerce.SubscriptionModule.Web
{
    public class Module : ModuleBase
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
            //_container.RegisterType<IUniqueNumberGenerator, SequenceUniqueNumberGeneratorServiceImpl>();
            _container.RegisterType<ISubscriptionService, SubscriptionServiceImpl>();
            _container.RegisterType<ISubscriptionSearchService, SubscriptionServiceImpl>();
            _container.RegisterType<IPaymentPlanService, PaymentPlanServiceImpl>();
            _container.RegisterType<ISubscriptionBuilder, SubscriptionBuilderImpl>();
            var observer = _container.Resolve<CreateSubscriptionObserver>();
            //Subscribe to the order change event. Try to create subscription for each new order
            _container.RegisterInstance<IObserver<OrderChangeEvent>>("CreateSubscriptionObserver", observer);

        }

        public override void PostInitialize()
        {
            base.PostInitialize();

            //Schedule periodic subscription processing job
            var settingsManager = _container.Resolve<ISettingsManager>();
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
        }

        #endregion


    }
}
