﻿using NUlid.Rng;
using System;

namespace NUlid.Tests;

/// <summary>
/// A fake RNG for the random part of ulid's.
/// </summary>
/// <remarks>
/// This can be used for unittests since it the returned 'random bytes' are known beforehand.
/// </remarks>
public class FakeUlidRng(byte[] desiredResult) : IUlidRng
{
    //Values specifically chosen to make the result spell DEADBEEFDEADBEEF
    public static readonly byte[] DEFAULTRESULT = [107, 148, 213, 185, 207, 107, 148, 213, 185, 207];
    private readonly byte[] _desiredresult = desiredResult;

    public FakeUlidRng()
        : this([.. DEFAULTRESULT]) { }     // make a copy

    public byte[] GetRandomBytes(DateTimeOffset dateTime) => _desiredresult;

    public void GetRandomBytes(Span<byte> buffer, DateTimeOffset dateTime) => _desiredresult.AsSpan().CopyTo(buffer);
}