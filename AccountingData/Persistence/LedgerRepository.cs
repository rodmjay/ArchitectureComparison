using System.Runtime.CompilerServices;
using AccountingDomain;
using DataOrientedArchitecture.Data.Entities;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace DataOrientedArchitecture.Data.Persistence;

public class LedgerRepository : ILedgerRepository
{
    private readonly LedgerContext _context;

    
    
    public LedgerRepository(LedgerContext context)
    {
        _context = context;
    }

    public async Task<AccountEntity> GetAccountForUpdateAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .FromSqlInterpolated($"SELECT * FROM Accounts WITH (UPDLOCK, ROWLOCK) WHERE Id = {accountId}")
            .AsTracking() // Need tracking to update later.
            .FirstAsync(cancellationToken);
    }


    //For 1,000 Transactions(Batch Size = 5,000) :
    //    Without Transaction: The mean execution time is approximately 116.33 ms.
    //    With Transaction: The mean execution time is approximately 60.87 ms.
    //For 10,000 Transactions(Batch Size = 5,000) :
    //    Without Transaction: The mean execution time is about 378.02 ms.
    //    With Transaction: The mean execution time is about 239.09 ms.

    public async Task DeleteLedgerAsync(Ledger ledger, int batchSize = 5000, 
        CancellationToken cancellationToken = default)
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
            SetOutputIdentity = true
        };

        // Run all deletes inside a single transaction.
        using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
        {
            // Delete child rows first.
            await _context.BulkDeleteAsync(entryEntities, bulkConfig, cancellationToken: cancellationToken);
            await _context.BulkDeleteAsync(transactionEntities, bulkConfig, cancellationToken: cancellationToken);
            await _context.BulkDeleteAsync(accountEntities, bulkConfig, cancellationToken: cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task<Ledger> LoadLedgerAsync(CancellationToken cancellationToken = default)
    {
        // Create a new Ledger instance where we'll load data.
        var loadedLedger = new Ledger();

        // Retrieve accounts from the database.
        var accountEntities = await _context.Accounts.ToListAsync(cancellationToken);
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
        var transactionEntities = await _context.Transactions.ToListAsync(cancellationToken);
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
        var entryEntities = await _context.Entries.ToListAsync(cancellationToken);
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
    public async Task SaveLedgerAsync(
        Ledger ledger,
        int batchSize = 5000,
        bool useTransaction = true,
        CancellationToken cancellationToken = default)
    {
        // 1) Map “to‑delete” keys into EF entities
        var delEntryEntities = ledger.DeletedEntryKeys
            .Select(k => new EntryEntity
            {
                TransactionId = k.TransactionId,
                AccountId = k.AccountId
            })
            .ToList();

        var delTransEntities = ledger.DeletedTransactionIds
            .Select(id => new TransactionEntity { Id = id })
            .ToList();

        var delAcctEntities = ledger.DeletedAccountIds
            .Select(id => new AccountEntity { Id = id })
            .ToList();

        // 2) Map your current/live domain objects
        var accountEntities = ledger.Accounts
            .Select(a => new AccountEntity
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type
            })
            .ToList();

        var transactionEntities = ledger.Transactions
            .Select(t => new TransactionEntity
            {
                Id = t.Id,
                Date = t.Date,
                Description = t.Description
            })
            .ToList();

        var entryEntities = ledger.Entries
            .Select(e => new EntryEntity
            {
                TransactionId = e.TransactionId,
                AccountId = e.AccountId,
                Amount = e.Amount
            })
            .ToList();

        // 3) Reset EF change‑tracker
        _context.ChangeTracker.Clear();

        // 4) Configure bulk options
        var bulkConfig = new BulkConfig
        {
            BatchSize = batchSize,
            UseTempDB = useTransaction,
            DoNotUpdateIfTimeStampChanged = true,
            SetOutputIdentity = true
        };

        // 5) Perform deletes then inserts inside a single TX
        if (useTransaction)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

            if (delEntryEntities.Any())
                await _context.BulkDeleteAsync(delEntryEntities, bulkConfig, cancellationToken: cancellationToken);

            if (delTransEntities.Any())
                await _context.BulkDeleteAsync(delTransEntities, bulkConfig, cancellationToken: cancellationToken);

            if (delAcctEntities.Any())
                await _context.BulkDeleteAsync(delAcctEntities, bulkConfig, cancellationToken: cancellationToken);

            // inserts
            await _context.BulkInsertAsync(accountEntities, bulkConfig, cancellationToken: cancellationToken);
            await _context.BulkInsertAsync(transactionEntities, bulkConfig, cancellationToken: cancellationToken);
            await _context.BulkInsertAsync(entryEntities, bulkConfig, cancellationToken: cancellationToken);

            await tx.CommitAsync(cancellationToken);
        }
        else
        {
            // without explicit transaction
            if (delEntryEntities.Any())
                await _context.BulkDeleteAsync(delEntryEntities, bulkConfig, cancellationToken: cancellationToken);
            if (delTransEntities.Any())
                await _context.BulkDeleteAsync(delTransEntities, bulkConfig, cancellationToken: cancellationToken);
            if (delAcctEntities.Any())
                await _context.BulkDeleteAsync(delAcctEntities, bulkConfig, cancellationToken: cancellationToken);

            await _context.BulkInsertAsync(accountEntities, bulkConfig, cancellationToken: cancellationToken);
            await _context.BulkInsertAsync(transactionEntities, bulkConfig, cancellationToken: cancellationToken);
            await _context.BulkInsertAsync(entryEntities, bulkConfig, cancellationToken: cancellationToken);
        }

        // 6) Clear the in‑memory trackers
        ledger.DeletedEntryKeys.Clear();
        ledger.DeletedTransactionIds.Clear();
        ledger.DeletedAccountIds.Clear();
    }


    public async IAsyncEnumerable<EntryEntity> StreamLedgerEntriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = _context.Entries.AsNoTracking();
        await foreach (var entry in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return entry;
        }
    }
}