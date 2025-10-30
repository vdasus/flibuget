using BenchmarkDotNet.Running;

namespace flibuget.Benchmarks;

public class Program
{
 public static void Main(string[] args)
 {
     BenchmarkRunner.Run<AudioTagServiceBenchmarks>();
 }
}
