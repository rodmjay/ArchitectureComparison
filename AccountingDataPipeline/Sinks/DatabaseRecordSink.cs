using AccountingDataPipeline.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountingDataPipeline.Sinks
{
    public class DatabaseRecordSink : IRecordSink<Record>
    {
        private readonly IDbContextFactory<PipelineDbContext> _dbContextFactory;

        public DatabaseRecordSink(IDbContextFactory<PipelineDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task WriteBatchAsync(IEnumerable<Record> records, CancellationToken cancellationToken = default)
        {
            // Create a new context instance for each write.
            using var context = _dbContextFactory.CreateDbContext();
            await context.Records.AddRangeAsync(records, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            // The context is disposed immediately after use so no extra change tracking persists.
        }
    }
}