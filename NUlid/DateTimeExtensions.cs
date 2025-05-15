using System;

namespace NUlid;

/// <summary>
/// Extension methods for <see cref="DateTime"/> and <see cref="DateTimeOffset"/> to create <see cref="Ulid"/>s.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="Ulid"/> based on the current <see cref="DateTime"/> value.
    /// </summary>
    /// <param name="dateTimeOffset">date time offset to base new ULID upon</param>
    /// <returns>A new <see cref="Ulid"/></returns>
    public static Ulid NewUlid(this DateTimeOffset dateTimeOffset)
        => Ulid.NewUlid(dateTimeOffset);

    /// <summary>
    /// Returns the minimum <see cref="Ulid"/> based on the current <see cref="DateTime"/> value.
    /// </summary>
    /// <param name="dateTimeOffset">date time offset to base new ULID upon</param>
    /// <returns>A new <see cref="Ulid"/></returns>
    public static Ulid MinUlid(this DateTimeOffset dateTimeOffset)
        => Ulid.MinAt(dateTimeOffset);

    /// <summary>
    /// Returns the maximum <see cref="Ulid"/> based on the current <see cref="DateTime"/> value.
    /// </summary>
    /// <param name="dateTimeOffset">date time offset to base new ULID upon</param>
    /// <returns>A new <see cref="Ulid"/></returns>
    public static Ulid MaxUlid(this DateTimeOffset dateTimeOffset)
        => Ulid.MaxAt(dateTimeOffset);
}
