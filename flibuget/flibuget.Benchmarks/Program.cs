using BenchmarkDotNet.Running;

namespace flibuget.Benchmarks;

public class Program
{
 public static void Main(string[] args)
 {
     Console.WriteLine("Start all benchmarks...");
     BenchmarkRunner.Run<AudioTagServiceBenchmarks>();
     Console.WriteLine("Done all benchmarks.");
    }
}
