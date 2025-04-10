using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace DataOrientedExample;

public class LedgerRepository
{
    private readonly LedgerContext _context;

    public LedgerRepository(LedgerContext context)
    {
        _context = context;
    }

    public async Task DeleteLedgerAsync(Ledger ledger, int batchSize = 5000)
    {
        // Map domain objects into EF entities.
        // Delete in the correct order: entries, transactions, then accounts.

        var entryEntities = new List<EntryEntity>();
        foreach (var entry in ledger.Entries)
        {
            entryEntities.Add(new EntryEntity
            {
                TransactionId = entry.TransactionId,
                AccountId = entry.AccountId,
                Amount = entry.Amount
            });
        }

        var transactionEntities = new List<TransactionEntity>();
        foreach (var tran in ledger.Transactions)
        {
            transactionEntities.Add(new TransactionEntity
            {
                Id = tran.Id,
                Date = tran.Date,
                Description = tran.Description
            });
        }

        var accountEntities = new List<AccountEntity>();
        foreach (var account in ledger.Accounts)
        {
            accountEntities.Add(new AccountEntity
            {
                Id = account.Id,
                Name = account.Name,
                Type = account.Type
            });
        }

        // Clear EF’s change tracker.
        _context.ChangeTracker.Clear();

        // Configure Bulk Delete settings.
        var bulkConfig = new BulkConfig
        {
            BatchSize = batchSize,
            UseTempDB = true,
            DoNotUpdateIfTimeStampChanged = true,
            SetOutputIdentity = true,
        };

        // Run all deletes inside a single transaction.
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            // Delete child rows first.
            await _context.BulkDeleteAsync(entryEntities, bulkConfig);
            await _context.BulkDeleteAsync(transactionEntities, bulkConfig);
            await _context.BulkDeleteAsync(accountEntities, bulkConfig);

            await transaction.CommitAsync();
        }
    }


    /// <summary>
    /// Loads the entire ledger from the database and maps the EF entities to DOA models.
    /// </summary>
    public async Task<Ledger> LoadLedgerAsync()
    {
        // Create a new Ledger instance where we'll load data.
        var loadedLedger = new Ledger();

        // Retrieve accounts from the database.
        var accountEntities = await _context.Accounts.ToListAsync();
        foreach (var ae in accountEntities)
        {
            loadedLedger.Accounts.Add(new Account
            {
                Id = ae.Id,
                Name = ae.Name,
                Type = ae.Type
            });
        }

        // Retrieve transactions.
        var transactionEntities = await _context.Transactions.ToListAsync();
        foreach (var te in transactionEntities)
        {
            loadedLedger.Transactions.Add(new Transaction
            {
                Id = te.Id,
                Date = te.Date,
                Description = te.Description
            });
        }

        // Retrieve entries.
        var entryEntities = await _context.Entries.ToListAsync();
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
    public async Task SaveLedgerAsync(Ledger ledger, int batchSize = 5000)
    {
        // Map your domain objects to EF entities.
        // (You might already have these mappings; adjust property names as needed.)

        var accountEntities = new List<AccountEntity>();
        foreach (var account in ledger.Accounts)
        {
            accountEntities.Add(new AccountEntity
            {
                // If you're letting SQL Server generate keys, you might omit setting Id
                Id = account.Id,
                Name = account.Name,
                Type = account.Type
            });
        }

        var transactionEntities = new List<TransactionEntity>();
        foreach (var tran in ledger.Transactions)
        {
            transactionEntities.Add(new TransactionEntity
            {
                Id = tran.Id,
                Date = tran.Date,
                Description = tran.Description
            });
        }

        var entryEntities = new List<EntryEntity>();
        foreach (var entry in ledger.Entries)
        {
            entryEntities.Add(new EntryEntity
            {
                TransactionId = entry.TransactionId,
                AccountId = entry.AccountId,
                Amount = entry.Amount
            });
        }

        // Clear any existing tracking
        _context.ChangeTracker.Clear();

        // Set up a BulkConfig object with a larger BatchSize and other optimizations.
        var bulkConfig = new BulkConfig
        {
            BatchSize = batchSize,  // A higher batch size may reduce database round-trips.
            UseTempDB = true,  // If your database recovery model permits minimal logging,
                               // this option can sometimes speed up bulk operations.
                               // Optionally, you can configure other settings such as BulkCopyTimeout.
            DoNotUpdateIfTimeStampChanged = true,
            SetOutputIdentity = true, // If you need to retrieve generated keys.
        };

        // Wrap all bulk operations inside a single transaction.
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            // Bulk insert accounts
            await _context.BulkInsertAsync(accountEntities, bulkConfig);

            // Bulk insert transactions
            await _context.BulkInsertAsync(transactionEntities, bulkConfig);

            // Bulk insert entries
            await _context.BulkInsertAsync(entryEntities, bulkConfig);

            await transaction.CommitAsync();
        }
    }

}