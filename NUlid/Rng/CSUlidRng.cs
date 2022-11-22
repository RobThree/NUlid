using System;
using System.Security.Cryptography;

namespace NUlid.Rng;

/// <summary>
/// A cryptographically secure RNG for the random part of ulid's.
/// </summary>
public class CSUlidRng : BaseUlidRng
{
#if !NET6_0_OR_GREATER
    // We only need one, single, instance of an RNG so we keep it around.
    private static readonly RNGCryptoServiceProvider _rng = new();
#endif

    /// <summary>
    /// Creates and returns cryptographically secure random bytes.
    /// </summary>
    /// <param name="dateTime">DateTime for which the random bytes need to be generated; is ignored.</param>
    /// <returns>Random bytes.</returns>
    public override byte[] GetRandomBytes(DateTimeOffset dateTime)
#if NET6_0_OR_GREATER
        => RandomNumberGenerator.GetBytes(RANDLEN);
#else
    {
        var buffer = new byte[RANDLEN];
        _rng.GetBytes(buffer);
        return buffer;
    }
#endif
}
