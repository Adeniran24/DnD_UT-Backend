using GameApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GameApi.Data
{
    public class PdfDbContext : DbContext
    {
        public PdfDbContext(DbContextOptions<PdfDbContext> options) : base(options) { }

        public DbSet<PdfFile> PdfFiles { get; set; }
    }
}