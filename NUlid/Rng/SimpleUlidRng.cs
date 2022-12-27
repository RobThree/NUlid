using System;
using System.Threading;

namespace NUlid.Rng;

/// <summary>
/// A simple RNG for the random part of ulid's.
/// </summary>
public class SimpleUlidRng : BaseUlidRng
{
    /// <summary>
    /// Creates and returns random bytes.
    /// </summary>
    /// <param name="dateTime">DateTime for which the random bytes need to be generated; is ignored.</param>
    /// <returns>Random bytes.</returns>
    public override byte[] GetRandomBytes(DateTimeOffset dateTime)
    {
        var _buffer = new byte[RANDLEN];
#if NET6_0_OR_GREATER
        Random.Shared.NextBytes(_buffer);
#else
        ThreadLocalRandom.NextBytes(_buffer);
#endif
        return _buffer;
    }

    /// <summary>
    /// Fills the <paramref name="buffer"/> with random bytes.
    /// </summary>
    /// <param name="buffer">The buffer to fill with random bytes.</param>
    /// <param name="dateTime">DateTime for which the random bytes need to be generated; is ignored.</param>
    /// <exception cref="ArgumentException">The buffer is too small.</exception>
    public override void GetRandomBytes(Span<byte> buffer, DateTimeOffset dateTime)
    {
        if (buffer.Length < RANDLEN)
        {
            Throw(buffer.Length);
            static void Throw(int len) => throw new ArgumentException($"The given buffer must be at least {RANDLEN} bytes long, actual: {len}");
        }

#if NET6_0_OR_GREATER
        Random.Shared.NextBytes(buffer);
#else
        var tmp = new byte[RANDLEN];
        ThreadLocalRandom.Instance.NextBytes(tmp);
        tmp.AsSpan().CopyTo(buffer);
#endif
    }

    /// <summary>
    /// Convenience class for dealing with randomness.
    /// </summary>
    /// <remarks>https://codeblog.jonskeet.uk/2009/11/04/revisiting-randomness/</remarks>
    private static class ThreadLocalRandom
    {
        /// <summary>
        /// Random number generator used to generate seeds,
        /// which are then used to create new random number
        /// generators on a per-thread basis.
        /// </summary>
        private static readonly Random _globalrandom = new();
        private static readonly object _globallock = new();

        /// <summary>
        /// Random number generator
        /// </summary>
        private static readonly ThreadLocal<Random> _threadrandom = new(NewRandom);

        /// <summary>
        /// Creates a new instance of Random. The seed is derived
        /// from a global (static) instance of Random, rather
        /// than time.
        /// </summary>
        public static Random NewRandom()
        {
            lock (_globallock)
            {
                return new Random(_globalrandom.Next());
            }
        }

        /// <summary>
        /// Returns an instance of Random which can be used freely
        /// within the current thread.
        /// </summary>
        public static Random Instance => _threadrandom.Value!;

        /// <summary>See <see cref="Random.Next()" /></summary>
        public static int Next() => Instance.Next();

        /// <summary>See <see cref="Random.Next(int)" /></summary>
        public static int Next(int maxValue) => Instance.Next(maxValue);

        /// <summary>See <see cref="Random.Next(int, int)" /></summary>
        public static int Next(int minValue, int maxValue) => Instance.Next(minValue, maxValue);

        /// <summary>See <see cref="Random.NextDouble()" /></summary>
        public static double NextDouble() => Instance.NextDouble();

        /// <summary>See <see cref="Random.NextBytes(byte[])" /></summary>
        public static void NextBytes(byte[] buffer) => Instance.NextBytes(buffer);
    }
}
