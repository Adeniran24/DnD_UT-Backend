using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameApi.Data
{
    public class PdfDbContextFactory : IDesignTimeDbContextFactory<PdfDbContext>
    {
        public PdfDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PdfDbContext>();
            
            // Itt add meg a MariaDB connection string-et
            optionsBuilder.UseMySql(
                "server=127.0.0.1;port=3306;database=pdfdb;user=gameuser;password=secretpassword;",
                new MySqlServerVersion(new Version(8, 0, 36))
            );

            return new PdfDbContext(optionsBuilder.Options);
        }
    }
}
