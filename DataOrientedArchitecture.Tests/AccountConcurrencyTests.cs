using DataOrientedExample;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DataOrientedArchitecture.Tests
{
    [TestFixture]
    public class AccountConcurrencyTests
    {
        private LedgerContext _context;        // Main context for setup/teardown.
        private int _testAccountId;              // ID of the test account record.

        // Define the connection string for the test database.
        private readonly string _connectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=DataOrientedExampleTests;Integrated Security=true;";

        [SetUp]
        public void SetUp()
        {
            // Create DbContextOptions using the connection string.
            var options = new DbContextOptionsBuilder<LedgerContext>()
                .UseSqlServer(_connectionString)
                .Options;

            // Initialize a new LedgerContext for this test.
            _context = new LedgerContext(options);

            // Optionally ensure a clean state:
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            // Create a sample AccountEntity for testing.
            var testAccount = new AccountEntity
            {
                Name = "Concurrency Test Account"
                // ... set other properties if needed ...
            };
            _context.Accounts.Add(testAccount);
            _context.SaveChanges();  // Insert the record into DB.

            _testAccountId = testAccount.Id;  // Save the generated ID.
        }
        [TearDown]
        public void TearDown()
        {
            if (_context != null)
            {
                // Create a new context instance so we get the latest state from the DB.
                var options = new DbContextOptionsBuilder<LedgerContext>()
                    .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=DataOrientedExampleTests;Integrated Security=true;")
                    .Options;

                using var freshContext = new LedgerContext(options);
                // Load the entity afresh using AsNoTracking so that we get the current RowVersion.
                var freshAccount = freshContext.Accounts
                    .AsNoTracking()
                    .SingleOrDefault(a => a.Id == _testAccountId);

                if (freshAccount != null)
                {
                    // Attach the fresh entity so EF Core will track it.
                    freshContext.Accounts.Attach(freshAccount);
                    freshContext.Accounts.Remove(freshAccount);
                    freshContext.SaveChanges();
                }
            }

            _context.Dispose();
        }


        [Test]
        public void Account_UpdateConcurrency_ThrowsException()
        {
            // Create options for contexts so they use the same connection string.
            var options = new DbContextOptionsBuilder<LedgerContext>()
                .UseSqlServer(_connectionString)
                .Options;

            // Load the same account in two separate contexts.
            using var context1 = new LedgerContext(options);
            using var context2 = new LedgerContext(options);

            var account1 = context1.Accounts.Single(a => a.Id == _testAccountId);
            var account2 = context2.Accounts.Single(a => a.Id == _testAccountId);

            // Modify the entity in the first context and save.
            account1.Name = "Updated Name (User1)";
            context1.SaveChanges();
            // At this point, the database row's RowVersion has been updated.

            // Modify the entity in the second context.
            account2.Name = "Updated Name (User2)";
            // Attempt to save changes in the second context - expect a concurrency exception.
            Assert.Throws<DbUpdateConcurrencyException>(() =>
            {
                context2.SaveChanges();
            }, "Expected a DbUpdateConcurrencyException due to row version mismatch");
        }
    }
}
