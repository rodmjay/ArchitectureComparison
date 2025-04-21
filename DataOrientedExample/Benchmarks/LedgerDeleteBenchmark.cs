using AccountingDomain;
using BenchmarkDotNet.Attributes;
using DataOrientedArchitecture.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DataOrientedArchitecture.Benchmarks.Benchmarks
{
    [MemoryDiagnoser]
    [ShortRunJob]

    public class LedgerDeleteBenchmark
    {
        // Parameter for the number of transactions to include in the ledger.
        [Params(100, 1000, 10000)]
        public int TransactionCount { get; set; }

        // Parameter for different Bulk Delete batch sizes.
        [Params(500, 1000, 5000)]
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

            // Populate the ledger with TransactionCount transactions.
            for (int i = 0; i < TransactionCount; i++)
            {
                ledger.PostTransaction(
                    DateTime.UtcNow,
                    $"Transaction {i}",
                    cashAccount, 100m,
                    revenueAccount, 100m
                );
            }

            // Set up a SQL Server connection using LocalDB.
            options = new DbContextOptionsBuilder<LedgerContext>()
                .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=AccountingPro;Integrated Security=true;")
                .Options;

            // Ensure the database schema is created.
            using (var context = new LedgerContext(options))
            {
                context.Database.EnsureCreated();
            }
        }

        // Pre-populate the database for the delete benchmark.
        // This runs only before the DeleteLedger benchmark.
        [IterationSetup(Target = nameof(DeleteLedger))]
        public void IterationSetupForDelete()
        {
            using (var context = new LedgerContext(options))
            {
                var repository = new LedgerRepository(context);
                repository.SaveLedgerAsync(ledger, BulkBatchSize)
                          .GetAwaiter().GetResult();
            }
        }

        // After each benchmark iteration, clean up the database.
        [IterationCleanup]
        public void IterationCleanup()
        {
            using (var context = new LedgerContext(options))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }
        }

        // Benchmark method for deleting the ledger.
        [Benchmark]
        public async Task DeleteLedger()
        {
            using (var context = new LedgerContext(options))
            {
                var repository = new LedgerRepository(context);
                await repository.DeleteLedgerAsync(ledger, BulkBatchSize);
            }
        }
    }
}
