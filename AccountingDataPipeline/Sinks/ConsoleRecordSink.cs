namespace AccountingDataPipeline.Sinks;

public class ConsoleRecordSink : IRecordSink<Record>
{
    public Task WriteBatchAsync(IEnumerable<Record> records, CancellationToken cancellationToken = default)
    {
        int count = records.Count();
        // In a real scenario, you might perform a bulk database insert here.
        Console.WriteLine($"Processing batch of {count} records.");
        return Task.CompletedTask;
    }
}
