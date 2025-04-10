﻿using Microsoft.EntityFrameworkCore;

namespace DataOrientedExample;

using Microsoft.EntityFrameworkCore;

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
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=AccountingPro;Integrated Security=true;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Optionally, configure table names, keys, relationships, etc.
        modelBuilder.Entity<AccountEntity>().ToTable("Accounts");
        modelBuilder.Entity<TransactionEntity>().ToTable("Transactions");
        modelBuilder.Entity<EntryEntity>().ToTable("Entries");
    }
}