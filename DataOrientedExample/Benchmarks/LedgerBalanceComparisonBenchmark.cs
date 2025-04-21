using AccountingDomain;
using BenchmarkDotNet.Attributes;

namespace DataOrientedArchitecture.Benchmarks.Benchmarks
{
    [MemoryDiagnoser]
    [ShortRunJob]
    public class LedgerBalanceComparisonBenchmark
    {
        // Parameterize the number of transactions; for example, 100,000.
        [Params(1000, 10_000)]
        public int TransactionCount { get; set; }

        // The account we want to recompute.
        private int targetAccountId;
        private Ledger ledger;

        [GlobalSetup]
        public void Setup()
        {
            // Create a new ledger.
            ledger = new Ledger();

            // Add two accounts (one for target and one secondary).
            targetAccountId = ledger.AddAccount("Target Account", AccountType.Asset);
            int otherAccountId = ledger.AddAccount("Other Account", AccountType.Expense);

            // Populate the ledger with TransactionCount transactions.
            // For example, each transaction debits the target and credits the other account.
            for (int i = 0; i < TransactionCount; i++)
            {
                ledger.PostTransaction(
                    DateTime.UtcNow,
                    $"Transaction {i}",
                    targetAccountId, 10m,    // Debit target account
                    otherAccountId, 10m      // Credit other account
                );
            }
        }

        /// <summary>
        /// Benchmark for the sequential recalculation method.
        /// </summary>
        [Benchmark(Baseline = true)]
        public decimal SequentialBalanceCalculation()
        {
            return ledger.RecalculateAccountBalanceSequential(targetAccountId);
        }

        /// <summary>
        /// Benchmark for the parallel recalculation method.
        /// </summary>
        [Benchmark]
        public decimal ParallelBalanceCalculation()
        {
            return ledger.RecalculateAccountBalanceParallel(targetAccountId);
        }
    }
}
