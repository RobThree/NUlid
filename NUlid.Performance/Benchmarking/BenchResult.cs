using System;

namespace NUlid.Performance.Benchmarking
{
    public class BenchResult
    {
        public TimeSpan Elapsed { get; set; }
        public int Iterations { get; set; }
    }
}