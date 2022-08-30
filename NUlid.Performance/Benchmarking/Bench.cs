using System;

namespace NUlid.Performance.Benchmarking;

public abstract class Bench : IBench
{
    public string Title { get; set; }
    public int? Iterations { get; private set; }
    public int? WarmupRounds { get; private set; }

    public Bench(string title, int? iterations = null, int? warmuprounds = null)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Iterations = iterations;
        WarmupRounds = warmuprounds;
    }

    public abstract void Execute(int iterations);
}