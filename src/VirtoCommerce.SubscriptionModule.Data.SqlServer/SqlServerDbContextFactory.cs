using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VirtoCommerce.SubscriptionModule.Data.Repositories;

namespace VirtoCommerce.SubscriptionModule.Data.SqlServer
{
    public class SqlServerDbContextFactory : IDesignTimeDbContextFactory<SubscriptionDbContext>
    {
        public SubscriptionDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<SubscriptionDbContext>();
            var connectionString = args.Any() ? args[0] : "Data Source=(local);Initial Catalog=VirtoCommerce3;Persist Security Info=True;User ID=virto;Password=virto;MultipleActiveResultSets=True;Connect Timeout=30";

            builder.UseSqlServer(
                connectionString,
                db => db.MigrationsAssembly(typeof(SqlServerDbContextFactory).Assembly.GetName().Name));

            return new SubscriptionDbContext(builder.Options);
        }
    }
}
