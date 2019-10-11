using System;

namespace NUlid.Rng
{
    /// <summary>
    /// Provides a baseclass for <see cref="IUlidRng"/>'s.
    /// </summary>
    public abstract class BaseUlidRng : IUlidRng
    {
        /// <summary>
        /// Default number of random bytes generated
        /// </summary>
        protected const int RANDLEN = 10;

        /// <summary>
        /// Creates and returns random bytes.
        /// </summary>
        /// <param name="dateTime">DateTime for which the random bytes need to be generated; can be ignored but provides context.</param>
        /// <returns>Random bytes.</returns>
        public abstract byte[] GetRandomBytes(DateTimeOffset dateTime);

        /// <summary>
        /// Returns the default <see cref="IUlidRng"/> used when no <see cref="IUlidRng"/> is specified.
        /// </summary>
        /// <remarks>
        /// This value references a <see cref="CSUlidRng"/>.
        /// </remarks>
        public static readonly IUlidRng DEFAULTRNG = new CSUlidRng();

        /// <summary>
        /// Creates and returns cryptographically secure Random bytes.
        /// </summary>
        /// <param name="dateTime">DateTime for which the random bytes need to be generated; can be ignored but provides context.</param>
        /// <returns>Random bytes.</returns>
        byte[] IUlidRng.GetRandomBytes(DateTimeOffset dateTime) => GetRandomBytes(dateTime);
    }
}
