using BenchmarkDotNet.Running;

namespace AccountingDataPipeline.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher([
                typeof(JsonParserBenchmarks),
                typeof(PipelineDatabaseBenchmark),
                typeof(PipelineBenchmark)
            ]);

            switcher.Run(args);

        }
    }
}
