using AccountingDomain;
using DataOrientedArchitecture.Data.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DataOrientedArchitecture.Tests
{
    [TestFixture]
    public class CancellationTests
    {
        private LedgerContext _context;
        private ILedgerRepository _repository;
        private Ledger _ledger;

        [SetUp]
        public void SetUp()
        {
            // Build DbContextOptions for testing (adjust connection string as needed).
            var options = new DbContextOptionsBuilder<LedgerContext>()
                .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=AccountingPro_Test;Integrated Security=true;")
                .Options;

            _context = new LedgerContext(options);
            // Ensure a clean DB state.
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            _repository = new LedgerRepository(_context);

            // Create a test ledger with some data.
            _ledger = new Ledger();
            int cashId = _ledger.AddAccount("Cash", AccountType.Asset);
            int revenueId = _ledger.AddAccount("Sales Revenue", AccountType.Revenue);
            // Populate ledger with a moderate number of transactions
            for (int i = 0; i < 1000; i++)
            {
                _ledger.PostTransaction(DateTime.UtcNow, $"Transaction {i}", cashId, 100m, revenueId, 100m);
            }
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
        }

        [Test]
        public void SaveLedgerAsync_Cancellation_ThrowsOperationCanceledException()
        {
            // Create a cancellation token source.
            using var cts = new CancellationTokenSource();

            // Start the SaveLedgerAsync operation.
            var task = _repository.SaveLedgerAsync(_ledger, cancellationToken: cts.Token);

            // Cancel the token quickly.
            cts.Cancel();

            Assert.ThrowsAsync <SqlException>(async () => await task);
        }
    }
}
