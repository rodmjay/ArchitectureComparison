using AccountingDataPipeline.Data;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace AccountingDataPipeline.Sinks
{
    public class BulkDatabaseRecordSink : IRecordSink<Record>
    {
        private readonly IDbContextFactory<PipelineDbContext> _dbContextFactory;

        public BulkDatabaseRecordSink(IDbContextFactory<PipelineDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task WriteBatchAsync(IEnumerable<Record> records, CancellationToken cancellationToken = default)
        {
            using var context = _dbContextFactory.CreateDbContext();
            // Bulk insert the batch of records. Ensure your project references EFCore.BulkExtensions.
            await context.BulkInsertAsync(records.ToList(), cancellationToken: cancellationToken);
        }
    }
}