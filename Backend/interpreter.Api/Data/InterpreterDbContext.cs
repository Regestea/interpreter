using interpreter.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace interpreter.Api.Data
{
    public class InterpreterDbContext : DbContext
    {
        public InterpreterDbContext(DbContextOptions<InterpreterDbContext> options) : base(options)
        {
        }
        
        public DbSet<VoiceEmbedding> VoiceEmbeddings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure your entities here
            // Example: modelBuilder.Entity<YourEntity>().HasKey(e => e.Id);
        }
    }
}

