using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;

namespace DataOrientedExample;

public class Program
{
    static async Task Main(string[] args)
    {
        //BenchmarkRunner.Run<LedgerBenchmark>();
        //BenchmarkRunner.Run<LedgerComputeBenchmark>();
        //BenchmarkRunner.Run<LedgerDeleteBenchmark>();
        BenchmarkRunner.Run<LedgerBalanceComparisonBenchmark>();
    }
}