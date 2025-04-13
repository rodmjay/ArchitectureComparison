using AccountingDataPipeline.Extensions;
using AccountingDataPipeline.Parsing;
using AccountingDataPipeline.Processing;
using AccountingDataPipeline.Sinks;

// Contains the BatchAsync extension method (see below)

namespace AccountingDataPipeline.Pipeline
{
    public class LargeFileProcessingPipeline
    {
        private readonly IJsonParser _jsonParser;
        private readonly IRecordTransformer<Record, Record> _transformer;
        private readonly IRecordSink<Record> _sink;
        private readonly int _batchSize;

        public LargeFileProcessingPipeline(IJsonParser jsonParser,
            IRecordTransformer<Record, Record> transformer,
            IRecordSink<Record> sink,
            int batchSize = 1000)
        {
            _jsonParser = jsonParser;
            _transformer = transformer;
            _sink = sink;
            _batchSize = batchSize;
        }

        public async Task ProcessFileAsync(Stream fileStream, CancellationToken cancellationToken = default)
        {
            var records = _jsonParser.ParseAsync<Record>(fileStream, cancellationToken);

            // Transform and batch process the records.
            await foreach (var batch in records.TransformAndBatchAsync(_transformer, _batchSize, cancellationToken))
            {
                await _sink.WriteBatchAsync(batch, cancellationToken);
            }
        }
    }
}