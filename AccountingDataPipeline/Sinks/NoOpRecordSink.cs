namespace AccountingDataPipeline.Sinks;

public class NoOpRecordSink : IRecordSink<Record>
{
    public Task WriteBatchAsync(IEnumerable<Record> records, CancellationToken cancellationToken = default)
    {
        // For benchmark purposes, do nothing.
        return Task.CompletedTask;
    }
}
