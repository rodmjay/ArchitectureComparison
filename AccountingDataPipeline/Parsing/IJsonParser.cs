namespace AccountingDataPipeline.Parsing;

public interface IJsonParser
{
    IAsyncEnumerable<T> ParseAsync<T>(Stream stream, CancellationToken cancellationToken = default);
}