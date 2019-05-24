using System;

namespace NUlid.Rng
{
    public abstract class BaseUlidRng : IUlidRng
    {
        protected const int RANDLEN = 10;
        
        public abstract byte[] GetRandomBytes(DateTimeOffset dateTime);
        public static readonly IUlidRng DEFAULTRNG = new CSUlidRng();

        /// <summary>
        /// Creates and returns cryptographically secure Random bytes.
        /// </summary>
        /// <param name="dateTime">DateTime for which the random bytes need to be generated; can be ignored but provides context</param>
        /// <returns>Random bytes.</returns>
        byte[] IUlidRng.GetRandomBytes(DateTimeOffset dateTime)
        {
            return GetRandomBytes(dateTime);
        }
    }
}
