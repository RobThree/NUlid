# ![Logo](https://raw.githubusercontent.com/RobThree/NUlid/master/logo.png) NUlid
A .Net [ULID](https://github.com/alizain/ulid/blob/master/README.md) implementation

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

You can also convert from/to GUID/UUID's, get the byte-representation of a ULID, create a ULID with specific timestamp and you can even specify an [`IUlidRng`](NUlid/Rng/IUlidRng.cs) to use for generating the randomness (by default NUlid uses the (slower, but cryptographically secure) [`CSUlidRng`](NUlid/Rng/CSUlidRng.cs) but a (faster) [`SimpleUlidRng`](NUlid/Rng/SimpleUlidRng.cs) is also provided). The ULID is implemented as a `struct` with (operator) overloads for (in)equality, comparison etc. built-in and is, generally, very much like .Net's native `Guid` struct. An extensive helpfile is provided in the Nuget package and [the testsuite](NUlid.Tests) also serves as a (simple) demonstration of NUlid's features.

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

Below measurements are based on an Intel(R) Xeon(R) CPU E3-1225 v3 @ 3.20GHz:

```
Guid.NewGuid():                  10.968.520/sec
UlidNet.Ulid.NewUlid():              78.378/sec *
Ulid.NewUlid(SimpleUlidRng):      5.355.601/sec
Ulid.NewUlid(CSUlidRng):          3.306.954/sec
Guid.Parse(string):               1.302.345/sec
Ulid.Parse(string):               1.498.890/sec
Guid.ToString():                  3.790.084/sec
Ulid.ToString():                  2.272.227/sec
new Guid(byte[]):                 6.334.916/sec
new Ulid(byte[]):                 5.484.964/sec
Guid.ToByteArray():               9.534.734/sec
Ulid.ToByteArray():               2.919.258/sec
Ulid.ToGuid():                    2.863.527/sec
new Ulid(Guid):                   7.434.209/sec
```
\* [fvilers/ulid.net](https://github.com/fvilers/ulid.net) (not included in repository, for reference only)

![Performance comparison](https://raw.githubusercontent.com/RobThree/NUlid/master/NUlid.Performance/results/comparison-v1.1.png)
