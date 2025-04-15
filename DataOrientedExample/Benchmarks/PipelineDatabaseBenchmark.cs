using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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

        // Benchmark for a small (1) and large (10,000) record set.
        [Params(1, 10_000)]
        public int RecordCount { get; set; }

        // Use the parameter to select the parser variant: "Old" for SystemTextJsonStreamParser, or "New" for LargeFileUtf8JsonParser.
        [Params("Old", "New")]
        public string ParserVariant { get; set; }

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
                context.Database.EnsureCreated();
            }

            // Instantiate the DbContextFactory.
            var dbContextFactory = new SimpleDbContextFactory(_dbOptions);

            // Select the JSON parser based on the benchmark parameter.
            IJsonParser jsonParser = ParserVariant switch
            {
                "Old" => new SystemTextJsonStreamParser(),
                "New" => new LargeFileUtf8JsonParser(),
                _ => throw new ArgumentException("Invalid parser variant")
            };

            // Set up the pipeline components.
            IRecordTransformer<Record, Record> transformer = new SampleRecordTransformer();
            IRecordSink<Record> sink = new DatabaseRecordSink(dbContextFactory);

            // Create the processing pipeline; adjust the batch size as desired.
            _pipeline = new LargeFileProcessingPipeline(jsonParser, transformer, sink, batchSize: 1000);

            // Generate synthetic JSON data.
            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
            var records = new Record[RecordCount];
            for (int i = 0; i < RecordCount; i++)
            {
                records[i] = new Record
                {
                    // Let SQL Server generate the Id.
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
            // Reset the JSON stream position before each benchmark iteration.
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

        // Entry point if you want to run from a console app.

    }
}
