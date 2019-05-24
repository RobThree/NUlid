using System;
using System.Security.Cryptography;

namespace NUlid.Rng
{
    /// <summary>
    /// A cryptographically secure (but slow(er)) RNG for the random part of ulid's.
    /// </summary>
    public class CSUlidRng : BaseUlidRng
    {
        // We only need one, single, instance of an RNG so we keep it around.
        private static readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        private readonly byte[] _buffer = new byte[RANDLEN];

        /// <summary>
        /// Creates and returns cryptographically secure random bytes.
        /// </summary>
        /// <param name="dateTime">DateTime for which the random bytes need to be generated; is ignored.</param>
        /// <returns>Random bytes.</returns>
        public override byte[] GetRandomBytes(DateTimeOffset dateTime)
        {
            _rng.GetBytes(_buffer);
            return _buffer;
        }
    }
}
