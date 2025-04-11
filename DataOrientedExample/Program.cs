using BenchmarkDotNet.Running;
using DataOrientedExample.Benchmarks;
using DataOrientedExample.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace DataOrientedExample;

public class Program
{
    static async Task Main(string[] args)
    {
        MappingConfig.RegisterMappings();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configure LedgerContext to use SQL Server, with no-tracking by default.
                services.AddDbContextPool<LedgerContext>(options =>
                    options.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=AccountingPro;Integrated Security=true;")
                        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

                // Register LedgerRepository.
                services.AddScoped<LedgerRepository>();

                // Other services can be registered here if needed.
            })
            .Build();

        //BenchmarkRunner.Run<LedgerBenchmark>();
        //BenchmarkRunner.Run<LedgerComputeBenchmark>();
        //BenchmarkRunner.Run<LedgerDeleteBenchmark>();
        //BenchmarkRunner.Run<LedgerBalanceComparisonBenchmark>();
        BenchmarkRunner.Run<TransactionSearchBenchmark>();
    }
}