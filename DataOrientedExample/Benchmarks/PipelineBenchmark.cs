using System.Text;
using System.Text.Json;
using AccountingDataPipeline;
using AccountingDataPipeline.Parsing;
using AccountingDataPipeline.Pipeline;
using AccountingDataPipeline.Processing;
using AccountingDataPipeline.Sinks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;

namespace Benchmarks.Benchmarks
{
    [MemoryDiagnoser]
    public class PipelineBenchmark
    {
        private LargeFileProcessingPipeline _pipeline;
        private MemoryStream _jsonStream;

        // Parameter: number of records to simulate.
        // Adjust RecordCount to simulate a larger file (e.g. 1GB worth of data).
        [Params(10_000,100_000,1_000_000)]
        public int RecordCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // Create instances for the pipeline.
            var jsonParser = new SystemTextJsonStreamParser();
            var transformer = new SampleRecordTransformer();
            var sink = new NoOpRecordSink(); // Use no-op for benchmarking
            _pipeline = new LargeFileProcessingPipeline(jsonParser, transformer, sink, batchSize: 1000);

            // Generate a JSON array with RecordCount records.
            // NOTE: Increase RecordCount to simulate a larger file.
            var options = new JsonSerializerOptions { WriteIndented = false };
            var records = new Record[RecordCount];
            for (int i = 0; i < RecordCount; i++)
            {
                records[i] = new Record
                {
                    Id = i,
                    Name = "Record " + i,
                    Value = i * 0.1
                };
            }
            // Serialize the records to JSON.
            string json = JsonSerializer.Serialize(records, options);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            _jsonStream = new MemoryStream(bytes);
            _jsonStream.Position = 0;
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Reset the stream position for each benchmark iteration.
            _jsonStream.Position = 0;
        }

        [Benchmark]
        public async Task ProcessFileAsync()
        {
            await _pipeline.ProcessFileAsync(_jsonStream);
        }
    }

}

