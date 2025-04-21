using BenchmarkDotNet.Running;
using DataOrientedArchitecture.Benchmarks.Benchmarks;

namespace DataOrientedArchitecture.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //MappingConfig.RegisterMappings();

            //var host = Host.CreateDefaultBuilder(args)
            //    .ConfigureServices((context, services) =>
            //    {
            //        services.AddDbContextPool<LedgerContext>(options =>
            //            options.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=AccountingPro;Integrated Security=true;")
            //                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

            //        services.AddScoped<LedgerRepository>();

            //        // If you're hosting an API, you could also add:
            //        // services.AddControllers();
            //    })
            //    .Build();

            // BenchmarkRunner.Run<PipelineDatabaseBenchmark>();
            //BenchmarkRunner.Run<PipelineBenchmark>();
            //BenchmarkRunner.Run<LedgerBenchmark>();
            //BenchmarkRunner.Run<LedgerBalanceComparisonBenchmark>();
            //BenchmarkRunner.Run<TransactionSearchBenchmark>();


            var switcher = new BenchmarkSwitcher([
                typeof(LedgerBenchmark),
                typeof(LedgerBalanceComparisonBenchmark),
                typeof(TransactionSearchBenchmark),
                typeof(SaveLedgerTransactionBenchmark)
            ]);

            switcher.Run(args);

        }
    }
}