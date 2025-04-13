using AccountingDomain;
using BenchmarkDotNet.Attributes;

namespace Benchmarks.Benchmarks
{
    [MemoryDiagnoser]
    public class TransactionSearchBenchmark
    {
        // Test with different ledger sizes.
        [Params(1000, 10000)]
        public int TransactionCount { get; set; }

        private Ledger ledger;

        [GlobalSetup]
        public void Setup()
        {
            // Create and populate the ledger.
            ledger = new Ledger();
            int cashId = ledger.AddAccount("Cash", AccountType.Asset);
            int revenueId = ledger.AddAccount("Sales Revenue", AccountType.Revenue);

            // Populate with TransactionCount transactions.
            for (int i = 0; i < TransactionCount; i++)
            {
                ledger.PostTransaction(DateTime.UtcNow, $"Transaction {i}", cashId, 100m, revenueId, 100m);
            }
        }

        [Benchmark(Baseline = true, Description = "SearchTransactions (ReadOnlySpan)")]
        public int SearchUsingSpan()
        {
            // Example predicate: select transactions with even Id.
            var span = ledger.SearchTransactions(t => t.Id % 2 == 0);
            return span.Length;
        }

        [Benchmark(Description = "SearchTransactionsCopy (List copy)")]
        public int SearchUsingCopy()
        {
            var list = ledger.SearchTransactionsCopy(t => t.Id % 2 == 0);
            return list.Count;
        }
    }
}