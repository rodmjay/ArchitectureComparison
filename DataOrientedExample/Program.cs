using AccountingData.Persistence;
using BenchmarkDotNet.Running;
using Benchmarks.Benchmarks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Benchmarks
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //MappingConfig.RegisterMappings();

            BenchmarkRunner.Run<PipelineDatabaseBenchmark>();


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

            //if (args.Contains("--benchmark"))
            //{
            //    BenchmarkRunner.Run<TransactionSearchBenchmark>();
            //}
            //else
            //{
            //    // Start the API endpoints.
            //    var app = host.Services.GetRequiredService<IHost>();
            //    // Map more endpoints as needed, for example:
            //    // app.MapPost("/import", async (HttpContext context, ILedgerRepository repo) => { ... });
            //    await app.RunAsync();
            //}
        }
    }
}