using Microsoft.EntityFrameworkCore;

namespace interpreter.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSet properties for your tables will be added here
        // Example: public DbSet<YourEntity> YourEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure your entities here
            // Example: modelBuilder.Entity<YourEntity>().HasKey(e => e.Id);
        }
    }
}

