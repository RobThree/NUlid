using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NUlid.Performance.Benchmarking
{
    public class SimpleBencher
    {
        public EventHandler<BenchRunningEventArgs> BenchRunning = (_, __) => { };
        public EventHandler<BenchRunningEventArgs> BenchWarmup = (_, __) => { };
        public EventHandler<BenchCompleteEventArgs> BenchComplete = (_, __) => { };

        public int Iterations { get; private set; }
        public int WarmupRounds { get; private set; }

        public SimpleBencher(int iterations, int warmuprounds = -1)
        {
            Iterations = iterations < 1 ? throw new ArgumentOutOfRangeException(nameof(iterations)) : iterations;
            WarmupRounds = warmuprounds < 0 ? (int)(iterations * 0.05) : warmuprounds;
        }

        public IEnumerable<BenchResult> BenchMark(IEnumerable<IBench> benchmarks)
        {
            return benchmarks.Select(b => BenchMark(b));
        }

        public BenchResult BenchMark(IBench bench)
        {
            var warmuprounds = bench.WarmupRounds ?? WarmupRounds;
            var iterations = bench.Iterations ?? Iterations;

            //Warmup
            if (warmuprounds > 0)
            {
                BenchWarmup(this, new BenchRunningEventArgs { Title = bench.Title, Iterations = warmuprounds });
                bench.Execute(warmuprounds);
            }

            // Give the test as good a chance as possible of avoiding garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            BenchRunning(this, new BenchRunningEventArgs { Title = bench.Title, Iterations = iterations });
            var s = Stopwatch.StartNew();
            bench.Execute(iterations);
            var e = s.Elapsed;

            var result = new BenchResult
            {
                Elapsed = e,
                Iterations = iterations
            };
            BenchComplete(this, new BenchCompleteEventArgs
            {
                Title = bench.Title,
                Result = result
            });

            return result;
        }
    }
}