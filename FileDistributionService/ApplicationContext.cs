using Microsoft.EntityFrameworkCore;

namespace FileDistributionService
{
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }
        public DbSet<FileModel> Files { get; set; }
    }
}
