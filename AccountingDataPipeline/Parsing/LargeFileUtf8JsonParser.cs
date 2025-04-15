using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AccountingDataPipeline; // For the Record type

namespace AccountingDataPipeline.Parsing
{
    public class LargeFileUtf8JsonParser : IJsonParser
    {
        public async IAsyncEnumerable<T> ParseAsync<T>(
            Stream stream,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Use the optimized Record parser if T is Record; otherwise, fall back.
            if (typeof(T) == typeof(Record))
            {
                await foreach (var rec in ParseRecordsAsync(stream, cancellationToken))
                {
                    yield return (T)(object)rec;
                }
            }
            else
            {
                await foreach (var item in ParseGenericAsync<T>(stream, cancellationToken))
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Parses a complete JSON array of Record objects.
        /// This demo implementation reads the entire stream into memory,
        /// uses JsonDocument.Parse with AllowTrailingCommas enabled,
        /// and then deserializes each element.
        /// </summary>
        private static async IAsyncEnumerable<Record> ParseRecordsAsync(
            Stream stream,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Read the entire stream into a MemoryStream.
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            byte[] bytes = ms.ToArray();

            // Configure options to allow trailing commas.
            var jsonDocOptions = new JsonDocumentOptions { AllowTrailingCommas = true };
            using JsonDocument doc = JsonDocument.Parse(bytes, jsonDocOptions);

            // Verify that the root element is a JSON array.
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new JsonException("Expected JSON array as root element.");

            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            // Iterate over each element in the array, deserialize and yield.
            foreach (JsonElement element in doc.RootElement.EnumerateArray())
            {
                // Deserialize using the element's raw JSON text.
                Record record = JsonSerializer.Deserialize<Record>(element.GetRawText(), serializerOptions);
                yield return record;
            }
        }

        /// <summary>
        /// Generic fallback parser for types other than Record.
        /// Uses the built-in asynchronous deserialization.
        /// </summary>
        private static async IAsyncEnumerable<T> ParseGenericAsync<T>(
            Stream stream,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true },
                cancellationToken))
            {
                if (item != null)
                {
                    yield return item;
                }
            }
        }
    }
}
