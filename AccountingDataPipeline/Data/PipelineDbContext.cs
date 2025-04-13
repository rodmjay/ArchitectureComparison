using Microsoft.EntityFrameworkCore;

namespace AccountingDataPipeline.Data
{
    public class PipelineDbContext : DbContext
    {
        public DbSet<Record> Records { get; set; }

        public PipelineDbContext(DbContextOptions<PipelineDbContext> options)
            : base(options)
        {
        }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Record>()
                .Property(r => r.Id)
                .ValueGeneratedOnAdd();
            base.OnModelCreating(modelBuilder);
        }

    }
}