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

TODO

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
|                      32_bit_uint_time_low                     |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|     16_bit_uint_time_high     |       16_bit_uint_random      |
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

Below measurements are based on a Intel(R) Xeon(R) CPU E3-1225 v3 @ 3.20GHz:

```
Guid.NewGuid():                                                    10.349.203/sec
Ulid.NewUlid():                                                     2.885.784/sec
Guid.Parse(string):                                                 1.245.751/sec
Ulid.Parse(string):                                                   385.934/sec
Guid.ToString():                                                    3.618.368/sec
Ulid.ToString():                                                    2.105.983/sec
new Guid(byte[]):                                                   6.168.259/sec
new Ulid(byte[]):                                                   4.852.037/sec
Guid.ToByteArray():                                                 9.083.221/sec
Ulid.ToByteArray():                                                 1.352.928/sec
Ulid.ToGuid():                                                      1.340.061/sec
new Ulid(Guid):                                                     6.096.758/sec
```

Note that these numbers will probably improve in the near future since there's enough room for optimization left.

## TODO:

- [ ] Finalize documentation -> Welcome page -> usage/quickstart
- [ ] ReadMe.md -> usage/quickstart
- [ ] Final NuGet package (currently RC status)
