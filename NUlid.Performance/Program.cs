using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NUlid.Rng;
using System;

namespace NUlid.Performance;

[MemoryDiagnoser]
public class Program
{
    private static readonly SimpleUlidRng _simplerng = new();
    private static readonly CSUlidRng _csrng = new();
    private static readonly MonotonicUlidRng _simplemonotonicrng = new(_simplerng);
    private static readonly MonotonicUlidRng _csmonotonicrng = new(_csrng);
    private static readonly Random _plainrng = new();

    public static void Main()
        => BenchmarkRunner.Run(typeof(Program).Assembly);

    private static byte[] GetRandomBytes(int amount)
    {
        var b = new byte[amount];
        _plainrng.NextBytes(b);
        return b;
    }

#pragma warning disable CA1822 // Mark members as static
    [Benchmark(Description = "Guid.NewGuid()")]
    public Guid Guid_NewGuid() => Guid.NewGuid();
    [Benchmark(Description = "Ulid.NewUlid(SimpleUlidRng)")]
    public Ulid Ulid_NewUlid_SimpleUlidRng() => Ulid.NewUlid(_simplerng);
    [Benchmark(Description = "Ulid.NewUlid(CSUlidRng)")]
    public Ulid Ulid_NewUlid_CSUlidRng() => Ulid.NewUlid(_csrng);
    [Benchmark(Description = "Ulid.NewUlid(SimpleMonotonicUlidRng)")]
    public Ulid Ulid_NewUlid_SimpleMonotonicUlidRng() => Ulid.NewUlid(_simplemonotonicrng);
    [Benchmark(Description = "Ulid.NewUlid(CSMonotonicUlidRng)")]
    public Ulid Ulid_NewUlid_CSMonotonicUlidRng() => Ulid.NewUlid(_csmonotonicrng);
    [Benchmark(Description = "Guid.Parse(string)")]
    public Guid Guid_Parse() => Guid.Parse(Guid.NewGuid().ToString());
    [Benchmark(Description = "Ulid.Parse(string)")]
    public Ulid Ulid_Parse() => Ulid.Parse(Ulid.NewUlid().ToString());
    [Benchmark(Description = "Guid.ToString()")]
    public string Guid_ToString() => Guid.NewGuid().ToString();
    [Benchmark(Description = "Ulid.ToString()")]
    public string Ulid_ToString() => Ulid.NewUlid().ToString();
    [Benchmark(Description = "new Guid(byte[])")]
    public Guid New_Guid_Byte() => new(GetRandomBytes(16));
    [Benchmark(Description = "new Ulid(byte[])")]
    public Ulid New_Ulid_Byte() => new(GetRandomBytes(16));
    [Benchmark(Description = "Guid.ToByteArray()")]
    public byte[] Guid_ToByteArray() => Guid.NewGuid().ToByteArray();
    [Benchmark(Description = "Ulid.ToByteArray()")]
    public byte[] Ulid_ToByteArray() => Ulid.NewUlid().ToByteArray();
    [Benchmark(Description = "Ulid.ToGuid()")]
    public Guid Ulid_ToGuid() => Ulid.NewUlid().ToGuid();
    [Benchmark(Description = "new Ulid(Guid)")]
    public Ulid New_Ulid_Guid() => new(Guid.NewGuid());
#pragma warning restore CA1822 // Mark members as static
}