using System;

namespace NUlid.Rng
{
    /// <summary>
    /// A simple (but fast(er)) RNG for the random part of ulid's.
    /// </summary>
    public class SimpleUlidRng : BaseUlidRng
    {
        // We only need one, single, instance of an RNG so we keep it around.
        private static readonly Random _rng = new Random();

        /// <summary>
        /// Creates and returns random bytes.
        /// </summary>
        /// <param name="dateTime">DateTime for which the random bytes need to be generated; is ignored.</param>
        /// <returns>Random bytes.</returns>
        public override byte[] GetRandomBytes(DateTimeOffset dateTime)
        {
            var _buffer = new byte[RANDLEN];
            _rng.NextBytes(_buffer);
            return _buffer;
        }
    }
}
