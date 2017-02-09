using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NUlid.Rng;
using System;
using System.Linq;

namespace NUlid.Performance
{
    class Program
    {
        static void Main(string[] args)
        {
            var benchmarks = new[] {
                typeof(NewGuidvsNewUlid),
                typeof(GuidParsevsUlidParse),
                typeof(GuidToStringvsUlidToString),
                typeof(GuidFromBytesvsUlidFromBytes),
                typeof(GuidToByteArrayvsUlidToByteArray),
                typeof(GuidToUlidAndViceVersa)
            };

            foreach (var b in benchmarks)
                BenchmarkRunner.Run(b);
        }
    }


    public class NewGuidvsNewUlid
    {
        private static readonly IUlidRng simplerng = new SimpleUlidRng();
        private static readonly IUlidRng csrng = new CSUlidRng();

        [Benchmark(Baseline = true, Description = "NewGuid()")]
        public Guid NewGuid()
        {
            return Guid.NewGuid();
        }

        [Benchmark(Baseline = false, Description = "NewUlid(SimpleUlidRng)")]
        public Ulid NewUlid_SimpleRNG()
        {
            return Ulid.NewUlid(simplerng);
        }

        [Benchmark(Baseline = false, Description = "NewUlid(CSUlidRng)")]
        public Ulid NewUlid_CSRNG()
        {
            return Ulid.NewUlid(csrng);
        }
    }

    public class GuidParsevsUlidParse
    {
        private const int testcount = 1000;
        private readonly string[] testguids;
        private readonly string[] testulids;

        public GuidParsevsUlidParse()
        {
            testguids = Enumerable.Range(0, testcount).Select(i => Guid.NewGuid().ToString()).ToArray();
            testulids = Enumerable.Range(0, testcount).Select(i => Ulid.NewUlid().ToString()).ToArray();
        }

        [Benchmark(Baseline = true, Description = "Guid.Parse(string)", OperationsPerInvoke = testcount)]
        public Guid[] GuidParse()
        {
            return testguids.Select(s => Guid.Parse(s)).ToArray();
        }

        [Benchmark(Baseline = false, Description = "Ulid.Parse(string)", OperationsPerInvoke = testcount)]
        public Ulid[] UlidParse()
        {
            return testulids.Select(s => Ulid.Parse(s)).ToArray();
        }

    }

    public class GuidToStringvsUlidToString
    {
        private const int testcount = 1000;
        private readonly Guid[] testguids;
        private readonly Ulid[] testulids;

        public GuidToStringvsUlidToString()
        {
            testguids = Enumerable.Range(0, testcount).Select(i => Guid.NewGuid()).ToArray();
            testulids = Enumerable.Range(0, testcount).Select(i => Ulid.NewUlid()).ToArray();
        }

        [Benchmark(Baseline = true, Description = "Guid.ToString()", OperationsPerInvoke = testcount)]
        public string[] GuidToString()
        {
            return testguids.Select(g => g.ToString()).ToArray();
        }

        [Benchmark(Baseline = false, Description = "Ulid.ToString()", OperationsPerInvoke = testcount)]
        public string[] UlidToString()
        {
            return testulids.Select(u => u.ToString()).ToArray();
        }
    }

    public class GuidFromBytesvsUlidFromBytes
    {
        private const int testcount = 1000;
        private readonly byte[][] data;

        public GuidFromBytesvsUlidFromBytes()
        {
            var r = new Random();
            data = Enumerable.Range(0, testcount).Select(n => { var b = new byte[16]; r.NextBytes(b); return b; }).ToArray();
        }

        [Benchmark(Baseline = true, Description = "new Guid(byte[])", OperationsPerInvoke = testcount)]
        public Guid[] GuidFromByteArray()
        {
            return data.Select(d => new Guid(d)).ToArray();
        }

        [Benchmark(Baseline = false, Description = "new Ulid(byte[])", OperationsPerInvoke = testcount)]
        public Ulid[] UlidFromByteArray()
        {
            return data.Select(d => new Ulid(d)).ToArray();
        }
    }

    public class GuidToByteArrayvsUlidToByteArray
    {
        private const int testcount = 1000;
        private readonly Guid[] testguids;
        private readonly Ulid[] testulids;

        public GuidToByteArrayvsUlidToByteArray()
        {
            testguids = Enumerable.Range(0, testcount).Select(i => Guid.NewGuid()).ToArray();
            testulids = Enumerable.Range(0, testcount).Select(i => Ulid.NewUlid()).ToArray();
        }

        [Benchmark(Baseline = true, Description = "Guid.ToByteArray()", OperationsPerInvoke = testcount)]
        public byte[][] GuidToByteArray()
        {
            return testguids.Select(g => g.ToByteArray()).ToArray();
        }

        [Benchmark(Baseline = false, Description = "Ulid.ToByteArray()", OperationsPerInvoke = testcount)]
        public byte[][] UlidToByteArray()
        {
            return testulids.Select(u => u.ToByteArray()).ToArray();
        }
    }

    public class GuidToUlidAndViceVersa
    {
        private const int testcount = 1000;
        private readonly Guid[] testguids;
        private readonly Ulid[] testulids;

        public GuidToUlidAndViceVersa()
        {
            testguids = Enumerable.Range(0, testcount).Select(i => Guid.NewGuid()).ToArray();
            testulids = Enumerable.Range(0, testcount).Select(i => Ulid.NewUlid()).ToArray();
        }

        [Benchmark(Description = "ToGuid()", OperationsPerInvoke = testcount)]
        public Guid[] ToGuid()
        {
            return testulids.Select(g => g.ToGuid()).ToArray();
        }

        [Benchmark(Description = "ToUlid()", OperationsPerInvoke = testcount)]
        public Ulid[] ToUlid()
        {
            return testguids.Select(g => new Ulid(g)).ToArray();
        }
    }
}
