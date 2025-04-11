using System.Runtime.CompilerServices;
using DataOrientedExample.Domain;
using DataOrientedExample.Entities;

namespace DataOrientedExample.Persistence;

public interface ILedgerRepository
{
    Task DeleteLedgerAsync(Ledger ledger, int batchSize = 5000,
        CancellationToken cancellationToken = default);

    Task<Ledger> LoadLedgerAsync(CancellationToken cancellationToken = default);

    Task SaveLedgerAsync(Ledger ledger, int batchSize = 5000, bool useTransaction = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams ledger entries as an asynchronous enumerable,
    /// allowing processing of each entry without loading all into memory.
    /// </summary>
    IAsyncEnumerable<EntryEntity> StreamLedgerEntriesAsync(CancellationToken cancellationToken = default);

}