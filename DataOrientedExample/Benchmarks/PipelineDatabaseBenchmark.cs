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

        // Adjust the connection string as needed.
        private const string ConnectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=BenchmarkDb;Trusted_Connection=True;MultipleActiveResultSets=true";

        [Params(1, 10_000)]
        public int RecordCount { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Configure EF Core to use SQL Server.
            _dbOptions = new DbContextOptionsBuilder<PipelineDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            // Ensure a clean database state.
            using (var context = new PipelineDbContext(_dbOptions))
            {
                context.Database.EnsureDeleted();
                // In production, you might call context.Database.Migrate();
                context.Database.EnsureCreated();
            }

            // Instantiate the DbContextFactory.
            var dbContextFactory = new SimpleDbContextFactory(_dbOptions);

            // Set up the pipeline components.
            IJsonParser jsonParser = new SystemTextJsonStreamParser();
            IRecordTransformer<Record, Record> transformer = new SampleRecordTransformer();

            // Use a sink that performs bulk insertion using EF Core Bulk Extensions.
            IRecordSink<Record> sink = new DatabaseRecordSink(dbContextFactory);

            // Create the pipeline; adjust the batch size as desired.
            _pipeline = new LargeFileProcessingPipeline(jsonParser, transformer, sink, batchSize: 1000);

            // Generate synthetic JSON data.
            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
            var records = new Record[RecordCount];
            for (int i = 0; i < RecordCount; i++)
            {
                records[i] = new Record
                {
                    // Do not set the Id; let SQL Server generate it.
                    Name = $"Record {i}",
                    Value = i * 0.1
                };
            }
            string json = JsonSerializer.Serialize(records, jsonOptions);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            _jsonStream = new MemoryStream(bytes);
            _jsonStream.Position = 0;
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Reset the JSON stream position.
            _jsonStream.Position = 0;

            // Recreate the database for a fresh state before each iteration.
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
