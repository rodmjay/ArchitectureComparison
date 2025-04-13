namespace AccountingDataPipeline.Sinks;

public interface IRecordSink<T>
{
    Task WriteBatchAsync(IEnumerable<T> records, CancellationToken cancellationToken = default);
}