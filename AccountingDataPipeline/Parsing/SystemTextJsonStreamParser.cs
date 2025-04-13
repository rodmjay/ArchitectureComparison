using System.Text.Json;

namespace AccountingDataPipeline.Parsing;

public class SystemTextJsonStreamParser : IJsonParser
{
    public async IAsyncEnumerable<T> ParseAsync<T>(Stream stream, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(stream, options, cancellationToken))
        {
            if (item != null)
            {
                yield return item;
            }
        }
    }
}