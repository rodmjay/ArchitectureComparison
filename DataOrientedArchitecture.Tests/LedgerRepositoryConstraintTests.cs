using System;
using System.Threading.Tasks;
using AccountingData.Persistence;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DataOrientedArchitecture.Tests
{
    [TestFixture]
    public class LedgerRepositoryConstraintTests
    {
        private LedgerContext _context;
        private ILedgerRepository _repository;

        // Define the test database connection string.
        private readonly string _connectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=AccountingPro_Test;Integrated Security=true;";

        [SetUp]
        public void SetUp()
        {
            // Create new DbContextOptions using the test connection string.
            var options = new DbContextOptionsBuilder<LedgerContext>()
                .UseSqlServer(_connectionString)
                .Options;

            _context = new LedgerContext(options);
            // Ensure we start with a clean database.
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            // IMPORTANT: Make sure your OnModelCreating is configured to add a UNIQUE index
            // on AccountEntity.Name to simulate a unique constraint violation.
            _repository = new LedgerRepository(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

    }
}
