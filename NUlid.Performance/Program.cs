using NUlid.Performance.Benchmarking;
using NUlid.Rng;
using System;
using System.Linq;

namespace NUlid.Performance
{
    class Program
    {
        static void Main(string[] args)
        {
            var simplerng = new SimpleUlidRng();
            var csrng = new CSUlidRng();
            var simplemonotonicrng = new MonotonicUlidRng(simplerng);
            var csmonotonicrng = new MonotonicUlidRng(csrng);
            var plainrng = new Random();

            var pt = new SimpleBencher(10000000);

            //pt.BenchRunning += (s, e) => Console.WriteLine($"Running {e.Title}, {e.Iterations:N0} iterations...");
            //pt.BenchWarmup += (s, e) => Console.WriteLine($"Warmup {e.Title}, {e.Iterations:N0} iterations...");
            pt.BenchComplete += (s, e) => Console.WriteLine($"Completed {e.Title,-40}: {e.Result.OperationsPerSecond(),15:N0}/sec.");

            var d = DateTimeOffset.Now;
            var benchtests = new IBench[] {
                new ForBench("Simple", () => simplerng.GetRandomBytes(d)),
                new ForBench("CS", () => csrng.GetRandomBytes(d)),

                new ForBench("Guid.NewGuid()", () => Guid.NewGuid()),
                new ForBench("Ulid.NewUlid(SimpleUlidRng)", () => Ulid.NewUlid(simplerng)),
                new ForBench("Ulid.NewUlid(CSUlidRng)", () => Ulid.NewUlid(csrng)),
                new ForBench("Ulid.NewUlid(SimpleMonotonicUlidRng)", () => Ulid.NewUlid(simplemonotonicrng)),
                new ForBench("Ulid.NewUlid(CSMonotonicUlidRng)", () => Ulid.NewUlid(csmonotonicrng)),
                new ForEachBench<string>("Guid.Parse(string)", (i) => Guid.Parse(i), (i) => Enumerable.Range(0, i).Select(n => Guid.NewGuid().ToString())),
                new ForEachBench<string>("Ulid.Parse(string)", (i) => Ulid.Parse(i), (i) => Enumerable.Range(0, i).Select(n => Ulid.NewUlid().ToString())),
                new ForEachBench<Guid>("Guid.ToString()", (i) => i.ToString(), (i) => Enumerable.Range(0, i).Select(n => Guid.NewGuid())),
                new ForEachBench<Ulid>("Ulid.ToString()", (i) => i.ToString(), (i) => Enumerable.Range(0, i).Select(n => Ulid.NewUlid())),
                new ForEachBench<byte[]>("new Guid(byte[])", (i) => new Guid(i), (i) => Enumerable.Range(0, i).Select(n => { var b = new byte[16]; plainrng.NextBytes(b); return b; })),
                new ForEachBench<byte[]>("new Ulid(byte[])", (i) => new Ulid(i), (i) => Enumerable.Range(0, i).Select(n => { var b = new byte[16]; plainrng.NextBytes(b); return b; })),
                new ForEachBench<Guid>("Guid.ToByteArray()", (i) => i.ToByteArray(), (i) => Enumerable.Range(0, i).Select(n => Guid.NewGuid())),
                new ForEachBench<Ulid>("Ulid.ToByteArray()", (i) => i.ToByteArray(), (i) => Enumerable.Range(0, i).Select(n => Ulid.NewUlid())),
                new ForEachBench<Ulid>("Ulid.ToGuid()", (i) => i.ToGuid(), (i) => Enumerable.Range(0, i).Select(n => Ulid.NewUlid())),
                new ForEachBench<Guid>("new Ulid(Guid)", (i) => new Ulid(i), (i) => Enumerable.Range(0, i).Select(n => Guid.NewGuid())),
            };

            var results = pt.BenchMark(benchtests).ToArray();

            Console.WriteLine("Done.");
        }
    }
}