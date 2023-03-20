using System.Reflection;
using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.SubscriptionModule.Data.Model;

namespace VirtoCommerce.SubscriptionModule.Data.Repositories
{
    public class SubscriptionDbContext : DbContextWithTriggers
    {
        public SubscriptionDbContext(DbContextOptions<SubscriptionDbContext> options)
            : base(options)
        {
        }

        protected SubscriptionDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region Subscription        
            modelBuilder.Entity<SubscriptionEntity>().ToTable("Subscription").HasKey(x => x.Id);
            modelBuilder.Entity<SubscriptionEntity>().Property(x => x.Id).HasMaxLength(128).ValueGeneratedOnAdd();
            #endregion

            #region PaymentPlan
            modelBuilder.Entity<PaymentPlanEntity>().ToTable("PaymentPlan").HasKey(x => x.Id);
            modelBuilder.Entity<PaymentPlanEntity>().Property(x => x.Id).HasMaxLength(128).ValueGeneratedOnAdd();
            #endregion

            base.OnModelCreating(modelBuilder);

            // Allows configuration for an entity type for different database types.
            // Applies configuration from all <see cref="IEntityTypeConfiguration{TEntity}" in VirtoCommerce.SubscriptionModule.Data.XXX project. /> 
            switch (this.Database.ProviderName)
            {
                case "Pomelo.EntityFrameworkCore.MySql":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.SubscriptionModule.Data.MySql"));
                    break;
                case "Npgsql.EntityFrameworkCore.PostgreSQL":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.SubscriptionModule.Data.PostgreSql"));
                    break;
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.SubscriptionModule.Data.SqlServer"));
                    break;
            }

        }
    }
}
