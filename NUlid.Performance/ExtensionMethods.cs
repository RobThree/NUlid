using NUlid.Performance.Benchmarking;

namespace NUlid.Performance;

public static class ExtensionMethods
{
    public static double OperationsPerSecond(this BenchResult result) => result.Iterations / result.Elapsed.TotalMilliseconds * 1000;
}