using System.Text;
using System.Text.Json;
using AccountingDataPipeline.Parsing;
using BenchmarkDotNet.Attributes;
// For Record
// For IJsonParser implementations

namespace AccountingDataPipeline.Benchmarks
{
    [MemoryDiagnoser]
    public class JsonParserBenchmarks
    {
        private MemoryStream _testDataStream;
        private const int RecordCount = 1000; // Adjust the workload as desired.

        // Use a parameter to choose the parser.
        [Params("Optimized", "SystemText")]
        public string ParserVariant { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // Generate a list of Record objects.
            var records = new List<Record>();
            for (int i = 0; i < RecordCount; i++)
            {
                records.Add(new Record
                {
                    Id = i,
                    Name = $"Record{i}",
                    Value = i * 1.0
                });
            }

            // Serialize the list into a JSON array.
            string jsonData = JsonSerializer.Serialize(records);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonData);

            // Create the MemoryStream from the JSON bytes.
            _testDataStream = new MemoryStream(jsonBytes);
            _testDataStream.Position = 0;
        }

        [Benchmark]
        public async Task ParseJson()
        {
            // Reset stream position before each benchmark iteration.
            _testDataStream.Position = 0;

            // Select the parser variant.
            IJsonParser parser = ParserVariant switch
            {
                "Optimized" => new LargeFileUtf8JsonParser(),
                "SystemText" => new SystemTextJsonStreamParser(),
                _ => throw new ArgumentException("Invalid parser variant")
            };

            // Consume all parsed records.
            await foreach (var record in parser.ParseAsync<Record>(_testDataStream))
            {
                // No operation; simply iterate to measure parsing time.
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _testDataStream?.Dispose();
        }
    }
}
