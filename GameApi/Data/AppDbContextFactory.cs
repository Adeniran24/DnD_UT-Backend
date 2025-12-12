using GameApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameApi.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            optionsBuilder.UseMySql(
                "server=127.0.0.1;port=3306;database=dnddb;user=root;password=udu2y7ULY?;",
                new MySqlServerVersion(new Version(8, 0, 36))
            );

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
