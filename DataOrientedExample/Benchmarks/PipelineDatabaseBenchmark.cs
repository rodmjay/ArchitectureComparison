using System.Text;
using System.Text.Json;
using AccountingDataPipeline;
using AccountingDataPipeline.Data;
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
    public class PipelineDatabaseBenchmark
    {
        private LargeFileProcessingPipeline _pipeline;
        private MemoryStream _jsonStream;
        private DbContextOptions<PipelineDbContext> _dbOptions;

        [Params(10_000, 100_000)]
        public int RecordCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Configure the in-memory database.
            _dbOptions = new DbContextOptionsBuilder<PipelineDbContext>()
                .UseInMemoryDatabase(databaseName: "BenchmarkDb")
                .Options;

            // Ensure the database is created.
            using (var context = new PipelineDbContext(_dbOptions))
            {
                context.Database.EnsureCreated();
            }

            // Create a new DbContextFactory.
            var dbContextFactory = new SimpleDbContextFactory(_dbOptions);

            // Set up the pipeline components.
            var jsonParser = new SystemTextJsonStreamParser();
            var transformer = new SampleRecordTransformer();

            // Use the factory-based sink.
            var sink = new DatabaseRecordSink(dbContextFactory);
            _pipeline = new LargeFileProcessingPipeline(jsonParser, transformer, sink, batchSize: 1000);

            // Generate synthetic JSON data.
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
            string json = JsonSerializer.Serialize(records, options);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            _jsonStream = new MemoryStream(bytes);
            _jsonStream.Position = 0;
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Reset the stream position.
            _jsonStream.Position = 0;

            // Recreate the in-memory database for a fresh state.
            using (var context = new PipelineDbContext(_dbOptions))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }
        }

        [Benchmark]
        public async Task ProcessAndSaveToDatabase()
        {
            await _pipeline.ProcessFileAsync(_jsonStream);
        }
    }

}
