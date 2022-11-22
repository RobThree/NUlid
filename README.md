# ![Logo](https://raw.githubusercontent.com/RobThree/NUlid/master/logo.png) NUlid
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
Or simply use the [Nuget](https://www.nuget.org/) package manager GUI in Visual Studio.

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

## Test Suite

[![Build status](https://ci.appveyor.com/api/projects/status/y4vvtyfi9qwvjclm?svg=true)](https://ci.appveyor.com/project/RobIII/nulid)

## Performance

Below measurements are based on an Intel(R) Core(TM) i9-10900X CPU @ 4.29Ghz:

```
BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.819)
Intel Core i9-10900X CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


|                               Method |      Mean |    Error |   StdDev |   Gen0 | Allocated |
|------------------------------------- |----------:|---------:|---------:|-------:|----------:|
|                       Guid.NewGuid() |  60.35 ns | 1.139 ns | 1.399 ns |      - |         - |
|          Ulid.NewUlid(SimpleUlidRng) |  63.34 ns | 0.332 ns | 0.277 ns | 0.0103 |     104 B |
|              Ulid.NewUlid(CSUlidRng) | 133.86 ns | 2.649 ns | 3.153 ns | 0.0103 |     104 B |
| Ulid.NewUlid(SimpleMonotonicUlidRng) |  79.50 ns | 1.034 ns | 0.967 ns | 0.0103 |     104 B |
|     Ulid.NewUlid(CSMonotonicUlidRng) |  78.78 ns | 1.360 ns | 1.272 ns | 0.0103 |     104 B |
|                   Guid.Parse(string) | 199.13 ns | 0.506 ns | 0.449 ns | 0.0095 |      96 B |
|                   Ulid.Parse(string) | 307.26 ns | 0.833 ns | 0.696 ns | 0.0625 |     632 B |
|                      Guid.ToString() | 175.19 ns | 0.996 ns | 0.932 ns | 0.0095 |      96 B |
|                      Ulid.ToString() | 217.47 ns | 2.649 ns | 2.477 ns | 0.0460 |     464 B |
|                   'new Guid(byte[])' |  11.37 ns | 0.038 ns | 0.033 ns | 0.0040 |      40 B |
|                   'new Ulid(byte[])' |  16.45 ns | 0.060 ns | 0.056 ns | 0.0040 |      40 B |
|                   Guid.ToByteArray() |  66.70 ns | 0.355 ns | 0.315 ns | 0.0039 |      40 B |
|                   Ulid.ToByteArray() | 144.38 ns | 0.579 ns | 0.513 ns | 0.0143 |     144 B |
|                        Ulid.ToGuid() | 146.24 ns | 1.198 ns | 1.062 ns | 0.0143 |     144 B |
|                     'new Ulid(Guid)' |  74.66 ns | 0.463 ns | 0.433 ns | 0.0039 |      40 B |
```