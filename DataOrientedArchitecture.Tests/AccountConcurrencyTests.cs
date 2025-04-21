using System.Runtime.CompilerServices;
using AccountingDomain;
using DataOrientedArchitecture.Data.Entities;
using DataOrientedArchitecture.Data.Persistence;
using EFCore.BulkExtensions;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace DataOrientedArchitecture.Tests
{
    public class LedgerRepository : ILedgerRepository
    {
        private readonly LedgerContext _context;

        public LedgerRepository(LedgerContext context)
        {
            _context = context;
        }

        // ------------------------------
        // CREATE
        // ------------------------------
        public async Task SaveLedgerAsync(Ledger ledger, int batchSize = 5000, bool useTransaction = true, CancellationToken cancellationToken = default)
        {
            // Map domain objects to EF entities using Mapster (or manual mapping, as desired).
            var accountEntities = ledger.Accounts.Adapt<List<AccountEntity>>();
            var transactionEntities = ledger.Transactions.Adapt<List<TransactionEntity>>();
            var entryEntities = ledger.Entries.Adapt<List<EntryEntity>>();

            // Clear any existing tracking.
            _context.ChangeTracker.Clear();

            var bulkConfig = new BulkConfig
            {
                BatchSize = batchSize,
                UseTempDB = useTransaction,  // Use TempDB if transactions are enabled.
                DoNotUpdateIfTimeStampChanged = true,
                SetOutputIdentity = true,
            };

            if (useTransaction)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
                {
                    await _context.BulkInsertAsync(accountEntities, bulkConfig, cancellationToken: cancellationToken);
                    await _context.BulkInsertAsync(transactionEntities, bulkConfig, cancellationToken: cancellationToken);
                    await _context.BulkInsertAsync(entryEntities, bulkConfig, cancellationToken: cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
            }
            else
            {
                await _context.BulkInsertAsync(accountEntities, bulkConfig, cancellationToken: cancellationToken);
                await _context.BulkInsertAsync(transactionEntities, bulkConfig, cancellationToken: cancellationToken);
                await _context.BulkInsertAsync(entryEntities, bulkConfig, cancellationToken: cancellationToken);
            }
        }

        // ------------------------------
        // READ
        // ------------------------------
        public async Task DeleteLedgerAsync(Ledger ledger, int batchSize = 5000,
            CancellationToken cancellationToken = default // Third parameter
        )
        {
            // Map domain objects to EF entities using Mapster (or manual mapping, as desired).
            var accountEntities = ledger.Accounts.Adapt<List<AccountEntity>>();
            var transactionEntities = ledger.Transactions.Adapt<List<TransactionEntity>>();
            var entryEntities = ledger.Entries.Adapt<List<EntryEntity>>();

            // Clear any existing tracking.
            _context.ChangeTracker.Clear();

            var bulkConfig = new BulkConfig { BatchSize = batchSize, UseTempDB = true };

            // Perform bulk delete for entries, transactions, and accounts.
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.BulkDeleteAsync(entryEntities, bulkConfig, cancellationToken: cancellationToken);
            await _context.BulkDeleteAsync(transactionEntities, bulkConfig, cancellationToken: cancellationToken);
            await _context.BulkDeleteAsync(accountEntities, bulkConfig, cancellationToken: cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public async Task<Ledger> LoadLedgerAsync(CancellationToken cancellationToken = default)
        {
            var loadedLedger = new Ledger();

            var accountEntities = await _context.Accounts.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var ae in accountEntities)
            {
                loadedLedger.Accounts.Add(new Account
                {
                    Id = ae.Id,
                    Name = ae.Name,
                    Type = ae.Type
                });
            }

            var transactionEntities = await _context.Transactions
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            foreach (var te in transactionEntities)
            {
                var transaction = new Transaction
                {
                    Id = te.Id,
                    Date = te.Date,
                    Description = te.Description
                };
                
                loadedLedger.Transactions.Add(transaction);
            }

            var entryEntities = await _context.Entries.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var ee in entryEntities)
            {
                loadedLedger.Entries.Add(new Entry
                {
                    TransactionId = ee.TransactionId,
                    AccountId = ee.AccountId,
                    Amount = ee.Amount
                });
            }

            return loadedLedger;
        }

        public async IAsyncEnumerable<EntryEntity> StreamLedgerEntriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var query = _context.Entries.AsNoTracking();
            await foreach (var entry in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return entry;
            }
        }
        
        // ------------------------------
        // UPDATE (example for bulk updates)
        // ------------------------------
        public async Task UpdateLedgerAsync(
            Ledger ledger, 
            int batchSize = 5000, 
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
        {
            // We assume that ledger objects have been modified in memory.
            // Map updated accounts.
            var accountEntities = ledger.Accounts.Adapt<List<AccountEntity>>();
            var transactionEntities = ledger.Transactions.Adapt<List<TransactionEntity>>();
            var entryEntities = ledger.Entries.Adapt<List<EntryEntity>>();

            _context.ChangeTracker.Clear();

            var bulkConfig = new BulkConfig
            {
                BatchSize = batchSize,
                UseTempDB = true,
                DoNotUpdateIfTimeStampChanged = true,
                SetOutputIdentity = true,
            };

            if (useTransaction)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                await _context.BulkUpdateAsync(accountEntities, bulkConfig, cancellationToken: cancellationToken);
                await _context.BulkUpdateAsync(transactionEntities, bulkConfig, cancellationToken: cancellationToken);
                await _context.BulkUpdateAsync(entryEntities, bulkConfig, cancellationToken: cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await _context.BulkUpdateAsync(accountEntities, bulkConfig, cancellationToken: cancellationToken);
                await _context.BulkUpdateAsync(transactionEntities, bulkConfig, cancellationToken: cancellationToken);
                await _context.BulkUpdateAsync(entryEntities, bulkConfig, cancellationToken: cancellationToken);
            }
        }

        // ------------------------------
        // Helper: Reload and attach an entity to ensure its RowVersion is current.
        // This follows the pattern your test uses.
        // ------------------------------
        public async Task<AccountEntity> ReloadAndAttachAccountAsync(int accountId, CancellationToken cancellationToken = default)
        {
            // Create fresh options (you can also inject a factory if needed)
            var options = new DbContextOptionsBuilder<LedgerContext>()
                .UseSqlServer(_context.Database.GetDbConnection().ConnectionString)
                .Options;
            using var freshContext = new LedgerContext(options);
            var freshAccount = await freshContext.Accounts.AsNoTracking()
                                    .SingleOrDefaultAsync(a => a.Id == accountId, cancellationToken);
            if (freshAccount != null)
            {
                // Attach the fresh entity to our current context.
                _context.Accounts.Attach(freshAccount);
            }
            return freshAccount;
        }
    }
}
