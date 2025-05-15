# ![Logo](https://raw.githubusercontent.com/RobThree/NUlid/master/logo.png) NUlid

![Build Status](https://img.shields.io/github/actions/workflow/status/RobThree/NUlid/test.yml?branch=master&style=flat-square) [![Nuget version](https://img.shields.io/nuget/v/NUlid.svg?style=flat-square)](https://www.nuget.org/packages/NUlid/)

A .Net [ULID](https://github.com/ulid/spec/blob/master/README.md) implementation

## Universally Unique Lexicographically Sortable Identifier

A GUID/UUID can be suboptimal for many use-cases because:

- It isn't the most character efficient way of encoding 128 bits
- It provides no other information than randomness

A ULID however:

- Is compatible with UUID/GUID's
- 1.21e+24 unique ULIDs per millisecond (1,208,925,819,614,629,174,706,176 to be exact)
- Lexicographically sortable
- Canonically encoded as a 26 character string, as opposed to the 36 character UUID
- Uses Crockford's base32 for better efficiency and readability (5 bits per character)
- Case insensitive
- No special characters (URL safe)

## Installation

```
PM> Install-Package NUlid
```
Or simply use the [Nuget](https://www.nuget.org/packages/NUlid) package manager GUI in Visual Studio.

### Usage

Creating a ULID:

```c#
// Create a ULID
var myulid = Ulid.NewUlid();
// Print ULID
Console.WriteLine(myulid);
```
Output:

`01ASB2XFCZJY7WHZ2FNRTMQJCT`

Parsing a ULID:

```c#
// Parse ULID:
var myulid = Ulid.Parse("01ASB2XFCZJY7WHZ2FNRTMQJCT");
// Print time-part of ULID:
Console.WriteLine(myulid.Time);
```
Output:

`4-8-2016 15:31:59 +00:00`

You can also convert from/to GUID/UUID's, get the byte-representation of a ULID, create a ULID with specific timestamp and you can even specify an [`IUlidRng`](NUlid/Rng/IUlidRng.cs) to use for generating the randomness (by default NUlid uses the [`CSUlidRng`](NUlid/Rng/CSUlidRng.cs) but a [`SimpleUlidRng`](NUlid/Rng/SimpleUlidRng.cs) is also provided, as well as a [`MonotonicUlidRng`](NUlid/Rng/MonotonicUlidRng.cs)). The ULID is implemented as a `struct` with (operator) overloads for (in)equality, comparison etc. built-in and is, generally, very much like .Net's native `Guid` struct. An extensive helpfile is provided in the Nuget package and [the testsuite](NUlid.Tests) also serves as a (simple) demonstration of NUlid's features.

## Specification

Below is the current specification of ULID as implemented in this repository.

```
 01AN4Z07BY      79KA1307SR9X4MV3
|----------|    |----------------|
 Timestamp          Randomness
  10 chars           16 chars
   48bits             80bits
   base32             base32
```

### Components

**Timestamp**
- 48 bit integer
- UNIX-time in milliseconds
- Won't run out of space till the year 10895 AD (this .Net specific Ulid implementation limits this to [DateTimeOffset.MaxValue](https://msdn.microsoft.com/en-us/library/system.datetimeoffset.maxvalue.aspx)).

**Randomness**
- 80 (Whenever possible: Cryptographically secure) Random bits

As of v1.4.0 [monotonic ULID's](https://github.com/ulid/spec#monotonicity) are supported (see below).

### Monotonicity
When generating a ULID within the same millisecond, it is possible to provide some guarantees regarding sort order (with some caveats). When you use the `MonotonicUlidRng` and a newly generated ULID in the same millisecond is detected, the random component is incremented by 1 bit in the least significant bit position (with carrying). For example: 

```c#
// Create monotonic rng
var rng = new MonotonicRng();

// Create ULIDs, assume that these calls occur within the same millisecond:
Console.WriteLine(Ulid.NewUlid(rng)); // 01DBN5W2SG000DCBVYHX4T6MCX
Console.WriteLine(Ulid.NewUlid(rng)); // 01DBN5W2SG000DCBVYHX4T6MCY
Console.WriteLine(Ulid.NewUlid(rng)); // 01DBN5W2SG000DCBVYHX4T6MCZ
Console.WriteLine(Ulid.NewUlid(rng)); // 01DBN5W2SG000DCBVYHX4T6MD0
Console.WriteLine(Ulid.NewUlid(rng)); // 01DBN5W2SG000DCBVYHX4T6MD1
Console.WriteLine(Ulid.NewUlid(rng)); // 01DBN5W2SG000DCBVYHX4T6MD2
```

By default the most significant bit of the random part is set to zero; this ensures you can generate enough ULID's after the initial one before causing an overflow. Some implementations simply pick a random value for the random part and increment this value, however, there's a (very small) chance that this random part is close to the overflow value. If you then happen to generate a lot of ULID's within the same millisecond there is a risk the you hit the overflow. By our method we ensure there's enough 'room' for new values before 'running out of values' (overflowing). It is, with some effort, even possible to 'resume counting' from any given ULID. 

### Encoding

[Crockford's Base32](http://www.crockford.com/wrmg/base32.html) is used as shown. This alphabet excludes the letters I, L, O, and U to avoid confusion and abuse.

```
0123456789ABCDEFGHJKMNPQRSTVWXYZ
```

### Binary Layout and Byte Order

The components are encoded as 16 octets. Each component is encoded with the Most Significant Byte first (network byte order).

```
0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                      32_bit_uint_time_high                    |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|     16_bit_uint_time_low      |       16_bit_uint_random      |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                       32_bit_uint_random                      |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                       32_bit_uint_random                      |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
```

### String Representation

```
ttttttttttrrrrrrrrrrrrrrrr
```

Where:
`t` is Timestamp
`r` is Randomness

## Prior Art

Based on / inspired by [alizain/ulid](https://github.com/alizain/ulid).

## Performance

Below measurements are based on an Intel(R) Core(TM) i9-10900X CPU @ 3.70Ghz:

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.4061)
Intel Core i9-10900X CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK 9.0.204
  [Host]     : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL
  DefaultJob : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL


| Method                               | Mean       | Error     | StdDev    | Gen0   | Allocated |
|------------------------------------- |-----------:|----------:|----------:|-------:|----------:|
| Guid.NewGuid()                       |  61.045 ns | 0.4488 ns | 0.3978 ns |      - |         - |
| Ulid.NewUlid(SimpleUlidRng)          |  35.331 ns | 0.2170 ns | 0.1694 ns |      - |         - |
| Ulid.NewUlid(CSUlidRng)              | 104.516 ns | 0.5099 ns | 0.4258 ns |      - |         - |
| Ulid.NewUlid(SimpleMonotonicUlidRng) |  51.985 ns | 0.3772 ns | 0.3344 ns |      - |         - |
| Ulid.NewUlid(CSMonotonicUlidRng)     |  52.000 ns | 0.1184 ns | 0.1050 ns |      - |         - |
| Guid.Parse(string)                   | 100.885 ns | 1.2808 ns | 1.1354 ns | 0.0095 |      96 B |
| Ulid.Parse(string)                   | 199.476 ns | 3.2495 ns | 3.0396 ns | 0.0181 |     184 B |
| Guid.ToString()                      |  76.089 ns | 0.8230 ns | 0.6426 ns | 0.0095 |      96 B |
| Ulid.ToString()                      | 131.441 ns | 0.5919 ns | 0.4943 ns | 0.0079 |      80 B |
| 'new Guid(byte[])'                   |   9.341 ns | 0.1644 ns | 0.1538 ns | 0.0040 |      40 B |
| 'new Ulid(byte[])'                   |  11.045 ns | 0.1987 ns | 0.1951 ns | 0.0040 |      40 B |
| Guid.ToByteArray()                   |  65.470 ns | 0.1393 ns | 0.1163 ns | 0.0039 |      40 B |
| Ulid.ToByteArray()                   | 111.239 ns | 0.9540 ns | 0.7966 ns | 0.0038 |      40 B |
| Ulid.ToGuid()                        | 106.292 ns | 0.2446 ns | 0.2043 ns |      - |         - |
| 'new Ulid(Guid)'                     |  65.200 ns | 0.2050 ns | 0.1712 ns |      - |         - |
```