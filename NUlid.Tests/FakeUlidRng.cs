using NUlid.Rng;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NUlid.Tests;

/// <summary>
/// A fake RNG for the random part of ulid's.
/// </summary>
/// <remarks>
/// This can be used for unittests since it the returned 'random bytes' are known beforehand.
/// </remarks>
public class FakeUlidRng : IUlidRng
{
    //Values specifically chosen to make the result spell DEADBEEFDEADBEEF
    public static readonly IReadOnlyList<byte> DEFAULTRESULT = new byte[] { 107, 148, 213, 185, 207, 107, 148, 213, 185, 207 };
    private readonly byte[] _desiredresult;

    public FakeUlidRng()
        : this(DEFAULTRESULT.ToArray()) { }

    public FakeUlidRng(byte[] desiredResult) => _desiredresult = desiredResult;

    public byte[] GetRandomBytes(DateTimeOffset dateTime) => _desiredresult;
}
