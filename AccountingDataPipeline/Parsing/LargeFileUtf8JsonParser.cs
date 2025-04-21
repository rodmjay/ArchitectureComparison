using System.Buffers;
using System.IO.Pipelines;
using System.Text.Json;

// For the Record type

namespace AccountingDataPipeline.Parsing
{
    public class LargeFileUtf8JsonParser : IJsonParser
    {
        public async IAsyncEnumerable<T> ParseAsync<T>(
            Stream stream,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Use the optimized Record parser if T is Record; otherwise, use the generic fallback.
            if (typeof(T) == typeof(Record))
            {
                await foreach (var rec in ParseRecordsAsync(stream, cancellationToken))
                    yield return (T)(object)rec;
            }
            else
            {
                await foreach (var item in ParseGenericAsync<T>(stream, cancellationToken))
                    yield return item;
            }
        }

        /// <summary>
        /// Optimized parser for Record objects.
        /// This incremental parser expects a JSON array as input.
        /// It uses direct deserialization from the Utf8JsonReader.
        /// </summary>
        private static async IAsyncEnumerable<Record> ParseRecordsAsync(
            Stream stream,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Create the PipeReader with leaveOpen:true so that the stream isn’t closed.
            var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(leaveOpen: true));
            JsonReaderState jsonState = default;
            bool insideArray = false;
            // Configure deserialization options. Optionally, you can set MaxDepth if needed.
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, MaxDepth = 64 };

            try
            {
                // Loop until no more data.
                while (true)
                {
                    ReadResult result = await reader.ReadAsync(cancellationToken);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    // If the stream is complete and there's no data, exit.
                    if (result.IsCompleted && buffer.IsEmpty)
                        break;

                    // Initialize a local consumed position.
                    SequencePosition consumed = buffer.Start;

                    // Process tokens from the current buffer.
                    while (true)
                    {
                        // Create a new Utf8JsonReader over the current buffer, using our saved state.
                        var jsonReader = new Utf8JsonReader(buffer, isFinalBlock: result.IsCompleted, state: jsonState);

                        // If we can't read a token, break out to await more data.
                        if (!jsonReader.Read())
                        {
                            break;
                        }

                        // Mark the consumed position for this token.
                        consumed = buffer.GetPosition(jsonReader.BytesConsumed);

                        // If we haven't yet encountered the array start, expect it.
                        if (!insideArray)
                        {
                            if (jsonReader.TokenType != JsonTokenType.StartArray)
                                throw new JsonException("Expected start of array in JSON input.");
                            insideArray = true;
                            // Update state and slice off the StartArray token.
                            jsonState = jsonReader.CurrentState;
                            buffer = buffer.Slice(jsonReader.BytesConsumed);
                            continue;
                        }

                        // If we encounter an EndArray token, update state, advance, and exit.
                        if (jsonReader.TokenType == JsonTokenType.EndArray)
                        {
                            jsonState = jsonReader.CurrentState;
                            consumed = buffer.GetPosition(jsonReader.BytesConsumed);
                            reader.AdvanceTo(consumed, buffer.End);
                            yield break;
                        }

                        // We expect a StartObject for each record.
                        if (jsonReader.TokenType != JsonTokenType.StartObject)
                        {
                            // Skip any tokens that are not StartObject.
                            jsonState = jsonReader.CurrentState;
                            buffer = buffer.Slice(jsonReader.BytesConsumed);
                            continue;
                        }

                        // At this point, we have a StartObject.
                        // Try to directly deserialize a Record from the current reader.
                        Record record;
                        try
                        {
                            record = JsonSerializer.Deserialize<Record>(ref jsonReader, options);
                        }
                        catch (JsonException)
                        {
                            // Likely due to incomplete data. Preserve state and break to await more.
                            jsonState = jsonReader.CurrentState;
                            break;
                        }

                        // Update state and consumed position.
                        jsonState = jsonReader.CurrentState;
                        consumed = buffer.GetPosition(jsonReader.BytesConsumed);
                        // Slice off the bytes for the record.
                        buffer = buffer.Slice(jsonReader.BytesConsumed);

                        yield return record;
                    }

                    // Advance the PipeReader so that processed data is released.
                    reader.AdvanceTo(consumed, buffer.End);

                    // If the read result is completed but some bytes remain, we treat that as an error.
                    if (result.IsCompleted)
                    {
                        if (!buffer.IsEmpty)
                            throw new JsonException("Incomplete JSON record at end of stream.");
                        break;
                    }
                }
            }
            finally
            {
                // Complete the PipeReader. The underlying stream remains open because leaveOpen:true.
                await reader.CompleteAsync();
            }
        }

        /// <summary>
        /// Generic fallback parser for types other than Record.
        /// Uses the built-in JsonSerializer.DeserializeAsyncEnumerable.
        /// </summary>
        private static async IAsyncEnumerable<T> ParseGenericAsync<T>(
            Stream stream,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken))
            {
                if (item != null)
                    yield return item;
            }
        }
    }
}
