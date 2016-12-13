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
        }

        #endregion


    }
}
