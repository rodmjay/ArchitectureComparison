using AccountingDomain;
using BenchmarkDotNet.Attributes;
using DataOrientedArchitecture.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DataOrientedArchitecture.Benchmarks.Benchmarks
{
    [MemoryDiagnoser]
    [ShortRunJob]

    public class SaveLedgerTransactionBenchmark
    {
        // Parameterize the number of transactions in the ledger.
        [Params(10, 100, 1000)]
        public int TransactionCount { get; set; }

        // Parameter for Bulk Batch Size.
        [Params(5000)]
        public int BatchSize { get; set; }

        // Parameter to test with transaction (true) or without (false).
        [Params(true, false)]
        public bool UseTransaction { get; set; }

        private Ledger ledger;
        private LedgerContext context;
        private LedgerRepository repository;

        [GlobalSetup]
        public void Setup()
        {
            // Initialize ledger.
            ledger = new Ledger();

            // Populate the ledger with TransactionCount transactions.
            // For simplicity, add 2 accounts.
            int cashId = ledger.AddAccount("Cash", AccountType.Asset);
            int revenueId = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

            for (int i = 0; i < TransactionCount; i++)
            {
                ledger.PostTransaction(
                    DateTime.UtcNow,
                    $"Transaction {i}",
                    cashId, 100m,
                    revenueId, 100m
                );
            }

            // Configure DbContext with your SQL Server connection string.
            var options = new DbContextOptionsBuilder<LedgerContext>()
                .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=AccountingPro;Integrated Security=true;")
                .Options;

            context = new LedgerContext(options);
            // Optional: ensure a fresh state.
            context.Database.EnsureCreated();

            repository = new LedgerRepository(context);
        }

        [IterationCleanup]
        public void CleanupIteration()
        {
            // Optionally, clear the database between iterations.
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        [Benchmark]
        public async Task SaveLedger()
        {
            // Measure the performance of SaveLedgerAsync with current parameters.
            await repository.SaveLedgerAsync(ledger, BatchSize, UseTransaction);
        }
    }
}
