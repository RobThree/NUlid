namespace NUlid.Performance.Benchmarking
{
    public interface IBench
    {
        string Title { get; set; }
        int? Iterations { get; }
        int? WarmupRounds { get; }

        void Execute(int iterations);
    }
}