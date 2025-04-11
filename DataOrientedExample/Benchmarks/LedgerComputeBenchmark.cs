using BenchmarkDotNet.Attributes;
using DataOrientedExample.Domain;

namespace DataOrientedExample.Benchmarks
{
    [MemoryDiagnoser]
    public class LedgerComputeBenchmark
    {
        // Parameter for BenchmarkDotNet – number of transactions in the ledger.
        [Params(100, 1000, 10000)]
        public int TransactionCount { get; set; }

        private Ledger ledger;
        private DateTime benchmarkNow;

        [GlobalSetup]
        public void Setup()
        {
            // We'll recreate a ledger with TransactionCount transactions.
            ledger = new Ledger();
            // Add a couple of accounts (for simplicity, two accounts)
            var cashAccount = ledger.AddAccount("Cash", AccountType.Asset);
            var revenueAccount = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

            // Define a date range (e.g. transactions over the past 30 days)
            DateTime startTime = DateTime.UtcNow.AddDays(-30);
            DateTime endTime = DateTime.UtcNow;

            // Create a deterministic Random instance for consistent benchmark runs.
            Random rand = new Random(42);

            for (int i = 0; i < TransactionCount; i++)
            {
                // Randomly pick a date between startTime and endTime.
                double totalSeconds = (endTime - startTime).TotalSeconds;
                DateTime randomDate = startTime.AddSeconds(rand.NextDouble() * totalSeconds);

                // For simplicity, each transaction has 100 debit and 100 credit.
                ledger.PostTransaction(randomDate, $"Transaction {i}",
                    cashAccount, 100m,    // Debit Cash
                    revenueAccount, 100m  // Credit Revenue
                );
            }

            // Also store a baseline "now" for the benchmark.
            benchmarkNow = DateTime.UtcNow;
        }

        /// <summary>
        /// Benchmark method to compute debits and credits over the past 7 days.
        /// It scans the in-memory ledger, correlates transactions with their dates,
        /// and then sums up debits (positive amounts) and credits (negative amounts)
        /// for transactions that have occurred in the target period.
        /// </summary>
        [Benchmark]
        public (decimal TotalDebit, decimal TotalCredit) ComputeDebitsCredits()
        {
            // Define the cutoff for the last 7 days
            DateTime cutoff = benchmarkNow.AddDays(-7);

            // Create a HashSet of transaction IDs that occurred within the target period.
            HashSet<int> validTransactionIds = ledger.Transactions
                .Where(t => t.Date >= cutoff)
                .Select(t => t.Id)
                .ToHashSet();

            // Compute the sum for debits (entries with positive amounts).
            decimal totalDebit = ledger.Entries
                .Where(e => validTransactionIds.Contains(e.TransactionId) && e.Amount > 0)
                .Sum(e => e.Amount);

            // Compute the sum for credits (entries with negative amounts).
            decimal totalCredit = ledger.Entries
                .Where(e => validTransactionIds.Contains(e.TransactionId) && e.Amount < 0)
                .Sum(e => e.Amount);

            return (totalDebit, totalCredit);
        }
    }
}
