using System.Text;
using System.Text.Json;
using AccountingDataPipeline.Data;
using AccountingDataPipeline.Parsing;
using AccountingDataPipeline.Pipeline;
using AccountingDataPipeline.Processing;
using AccountingDataPipeline.Sinks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace AccountingDataPipeline.Tests
{
    [TestFixture]
    public class PipelineTests
    {
        private DbContextOptions<PipelineDbContext> _dbOptions;
        private IDbContextFactory<PipelineDbContext> _dbContextFactory;
        // Adjust this connection string as needed (this example uses LocalDB)
        private const string ConnectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=TestBenchmarkDb;Trusted_Connection=True;MultipleActiveResultSets=true";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Build EF Core options for SQL Server.
            _dbOptions = new DbContextOptionsBuilder<PipelineDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            // Ensure a clean database.
            using (var context = new PipelineDbContext(_dbOptions))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }

            // Instantiate the DbContext factory.
            _dbContextFactory = new SimpleDbContextFactory(_dbOptions);
        }

        [SetUp]
        public void Setup()
        {
            // Clear data from the Records table before each test.
            using (var context = new PipelineDbContext(_dbOptions))
            {
                context.Database.ExecuteSqlRaw("DELETE FROM [Records]");
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Clean up the database after tests are complete.
            using (var context = new PipelineDbContext(_dbOptions))
            {
                context.Database.EnsureDeleted();
            }
        }

        [Test]
        public async Task ProcessFileAsync_Should_Insert_All_Records_Using_BulkExtensions()
        {
            // Arrange
            const int recordCount = 10000;
            var records = new Record[recordCount];

            // Create synthetic records with no Id (so it gets generated).
            for (int i = 0; i < recordCount; i++)
            {
                records[i] = new Record
                {
                    Name = $"Record {i}",
                    Value = i * 0.1
                };
            }

            // Serialize the array of records to JSON.
            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };
            string json = JsonSerializer.Serialize(records, jsonOptions);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            // Wrap the JSON bytes in a MemoryStream.
            using var jsonStream = new MemoryStream(jsonBytes);

            // Set up pipeline components.
            IJsonParser jsonParser = new SystemTextJsonStreamParser();
            IRecordTransformer<Record, Record> transformer = new SampleRecordTransformer();
            IRecordSink<Record> sink = new BulkDatabaseRecordSink(_dbContextFactory);

            // Create the pipeline with a desired batch size (e.g., 1000).
            var pipeline = new LargeFileProcessingPipeline(jsonParser, transformer, sink, batchSize: 1000);

            // Act: Process the JSON stream.
            await pipeline.ProcessFileAsync(jsonStream);

            // Assert: Verify that all records were inserted.
            using (var context = new PipelineDbContext(_dbOptions))
            {
                int insertedCount = await context.Records.CountAsync();
                ClassicAssert.AreEqual(recordCount, insertedCount, "All records should be inserted into the database");
            }
        }
    }
}
