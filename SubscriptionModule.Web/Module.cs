using Microsoft.Practices.Unity;
using SubscriptionModule.Core.Services;
using SubscriptionModule.Data.Repositories;
using SubscriptionModule.Data.Services;
using SubscriptionModule.Data.Migrations;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;
using VirtoCommerce.Domain.Commerce.Model;
using SubscriptionModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

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

        }

        public override void PostInitialize()
        {
            base.PostInitialize();        

        }

        #endregion

  
    }
}
