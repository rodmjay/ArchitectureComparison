using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AccountingDataPipeline.Parsing;

public class SystemTextJsonStreamParser : IJsonParser
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public async IAsyncEnumerable<T> ParseAsync<T>(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(stream, Options, cancellationToken))
        {
            if (item != null)
            {
                yield return item;
            }
        }
    }
}

