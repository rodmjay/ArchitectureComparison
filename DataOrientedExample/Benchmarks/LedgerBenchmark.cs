using AccountingData.Persistence;
using AccountingDomain;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;

namespace Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class LedgerBenchmark
{
    // Parameter for number of transactions.
    [Params(100, 1000, 10000)]
    public int TransactionCount { get; set; }

    // New parameter for testing different BatchSize values.
    [Params(5000)]
    public int BulkBatchSize { get; set; }

    private Ledger ledger;
    private DbContextOptions<LedgerContext> options;

    [GlobalSetup]
    public void Setup()
    {
        // Create a new ledger and add two accounts.
        ledger = new Ledger();
        var cashAccount = ledger.AddAccount("Cash", AccountType.Asset);
        var revenueAccount = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

        // Add TransactionCount transactions to the ledger.
        for (int i = 0; i < TransactionCount; i++)
        {
            ledger.PostTransaction(
                DateTime.UtcNow,
                $"Transaction {i}",
                cashAccount, 100m,    // Debit Cash
                revenueAccount, 100m  // Credit Revenue
            );
        }

        // Set up the SQL Server database connection using LocalDB.
        options = new DbContextOptionsBuilder<LedgerContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=AccountingPro;Integrated Security=true;")
            .Options;

        // Create the database schema in the SQL Server database.
        using (var context = new LedgerContext(options))
        {
            context.Database.EnsureCreated();
        }
    }

    // After each benchmark iteration, reset the database schema.
    [IterationCleanup]
    public void CleanupIteration()
    {
        using (var context = new LedgerContext(options))
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
    }

    // Benchmark method for bulk inserting the ledger using parameterized BatchSize.
    [Benchmark]
    public async Task BulkInsertLedger()
    {
        using (var context = new LedgerContext(options))
        {
            var repository = new LedgerRepository(context);
            await repository.SaveLedgerAsync(ledger, BulkBatchSize);
        }
    }

    // Iteration setup for the load benchmark.
    [IterationSetup(Target = nameof(LoadLedger))]
    public void IterationSetupForLoad()
    {
        using (var context = new LedgerContext(options))
        {
            var repository = new LedgerRepository(context);
            repository.SaveLedgerAsync(ledger, BulkBatchSize).GetAwaiter().GetResult();
        }
    }

    // Benchmark method for loading the ledger from the database.
    [Benchmark]
    public async Task LoadLedger()
    {
        using (var context = new LedgerContext(options))
        {
            var repository = new LedgerRepository(context);
            await repository.LoadLedgerAsync();
        }
    }
}
