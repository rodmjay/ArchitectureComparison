using DataOrientedArchitecture.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataOrientedArchitecture.Data.Persistence;

public class LedgerContext : DbContext
{
    public LedgerContext()
    {
    }

    // New constructor for passing options during benchmarking (or tests)
    public LedgerContext(DbContextOptions<LedgerContext> options)
        : base(options)
    {
    }

    public DbSet<AccountEntity> Accounts { get; set; }
    public DbSet<TransactionEntity> Transactions { get; set; }
    public DbSet<EntryEntity> Entries { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Optionally, configure table names, keys, relationships, etc.
        modelBuilder.Entity<AccountEntity>().ToTable("Accounts");
        modelBuilder.Entity<TransactionEntity>().ToTable("Transactions");
        modelBuilder.Entity<EntryEntity>().ToTable("Entries");

        modelBuilder.Entity<AccountEntity>()
            .HasIndex(a => a.Name)
            .IsUnique();
    }
}