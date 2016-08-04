using System;

namespace NUlid.Rng
{
    /// <summary>
    /// A simple (but fast(er)) RNG for the random part of ulid's.
    /// </summary>
    public class SimpleUlidRng : IUlidRng
    {
        // We only need one, single, instance of an RNG so we keep it around.
        private static readonly Random _rng = new Random();

        /// <summary>
        /// Creates and returns the specified number of random bytes.
        /// </summary>
        /// <param name="length">The desired number of random bytes.</param>
        /// <returns>The specified number of random bytes.</returns>
        public byte[] GetRandomBytes(int length)
        {
            var result = new byte[length];
            _rng.NextBytes(result);
            return result;
        }

        /// <summary>
        /// Creates and returns the specified number of random bytes.
        /// </summary>
        /// <param name="length">The desired number of random bytes.</param>
        /// <returns>The specified number of random bytes.</returns>
        byte[] IUlidRng.GetRandomBytes(int length)
        {
            return this.GetRandomBytes(length);
        }
    }
}
