using System.Security.Cryptography;

namespace NUlid.Rng
{
    /// <summary>
    /// A cryptographically secure (but slow(er)) RNG for the random part of ulid's.
    /// </summary>
    public class CSUlidRng : IUlidRng
    {
#if NETSTANDARD1_3
        // We only need one, single, instance of an RNG so we keep it around.
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
#else
        // We only need one, single, instance of an RNG so we keep it around.
        private static readonly RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
#endif

        /// <summary>
        /// Creates and returns the specified number of random bytes.
        /// </summary>
        /// <param name="length">The desired number of random bytes.</param>
        /// <returns>The specified number of random bytes.</returns>
        public byte[] GetRandomBytes(int length)
        {
            var result = new byte[length];
            _rng.GetBytes(result);
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
