using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SubscriptionModule.Data.Model;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Data.Infrastructure;
using VirtoCommerce.Platform.Data.Infrastructure.Interceptors;

namespace SubscriptionModule.Data.Repositories
{
    public class SubscriptionRepositoryImpl : EFRepositoryBase, IRepository, IDisposable, ISubscriptionRepository
    {
        public SubscriptionRepositoryImpl()
        {
        }

        public SubscriptionRepositoryImpl(string nameOrConnectionString, params IInterceptor[] interceptors)
            : base(nameOrConnectionString, null, interceptors)
        {
            Configuration.LazyLoadingEnabled = false;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            #region SubscriptionCustomerOrders
            modelBuilder.Entity<SubscriptionOrderRelationEntity>().HasKey(x => x.Id)
                .Property(x => x.Id);
            modelBuilder.Entity<SubscriptionOrderRelationEntity>().ToTable("SubscriptionOrderRelation");
            modelBuilder.Entity<SubscriptionOrderRelationEntity>().HasRequired(x => x.Subscription)
                                                 .WithMany(x => x.OrderRelations).HasForeignKey(x => x.SubscriptionId)
                                                 .WillCascadeOnDelete(true);       
            #endregion

            #region Subscription        
            modelBuilder.Entity<SubscriptionEntity>().HasKey(x => x.Id)
                .Property(x => x.Id);
            modelBuilder.Entity<SubscriptionEntity>().ToTable("Subscription");           
            #endregion

            #region PaymentPlan
            modelBuilder.Entity<PaymentPlanEntity>().HasKey(x => x.Id)
            .Property(x => x.Id);
            modelBuilder.Entity<PaymentPlanEntity>().ToTable("PaymentPlan");
            #endregion

            base.OnModelCreating(modelBuilder);
        }

        #region ISubscriptionRepository members    

        public IQueryable<PaymentPlanEntity> PaymentPlans
        {
            get { return GetAsQueryable<PaymentPlanEntity>(); }
        }

        public IQueryable<SubscriptionEntity> Subscriptions
        {
            get { return GetAsQueryable<SubscriptionEntity>(); }
        }

        public PaymentPlanEntity[] GetPaymentPlansByIds(string[] ids)
        {
            var query = PaymentPlans.Where(x => ids.Contains(x.Id));
            return query.ToArray();
        }

        public SubscriptionEntity[] GetSubscriptionsByIds(string[] ids, string responseGroup = null)
        {
            var result = Subscriptions.Include(x=>x.OrderRelations).Where(x => ids.Contains(x.Id)).ToArray();
            return result;
        }

        public void RemovePaymentPlansByIds(string[] ids)
        {
            var paymentPlans = GetPaymentPlansByIds(ids);
            foreach (var paymentPlan in paymentPlans)
            {
                Remove(paymentPlan);
            }
        }

        public void RemoveSubscriptionsByIds(string[] ids)
        {
            var subscriptions = GetSubscriptionsByIds(ids);
            foreach (var subscription in subscriptions)
            {
                Remove(subscription);
            }
        } 
        #endregion
    }
}
