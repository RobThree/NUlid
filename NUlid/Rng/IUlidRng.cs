using System;

namespace NUlid.Rng;

/// <summary>
/// Defines the interface for ulid Random Number Generators.
/// </summary>
public interface IUlidRng
{
    /// <summary>
    /// Creates and returns random bytes.
    /// </summary>
    /// <param name="dateTime">DateTime for which the random bytes need to be generated; can be ignored but provides context.</param>
    /// <returns>Random bytes.</returns>
    byte[] GetRandomBytes(DateTimeOffset dateTime);

    /// <summary>
    /// Fills the <paramref name="buffer"/> with random bytes.
    /// </summary>
    /// <param name="buffer">The buffer to fill with random bytes.</param>
    /// <param name="dateTime">DateTime for which the random bytes need to be generated; can be ignored but provides context.</param>
    void GetRandomBytes(Span<byte> buffer, DateTimeOffset dateTime);
}
