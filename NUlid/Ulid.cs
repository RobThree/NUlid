using NUlid.Rng;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace NUlid
{
    /// <summary>
    /// Represents a <see cref="Ulid"/> (Universally Unique Lexicographically Sortable Identifier), based/inspired on
    /// <see href="https://github.com/alizain/ulid">alizain/ulid</see>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [TypeConverter(typeof(UlidTypeConverter))]
    [Serializable]
    [ComVisible(true)]
    [DebuggerDisplay("{ToString(),nq}")]
    public struct Ulid : IEquatable<Ulid>, IComparable<Ulid>, IComparable, ISerializable, IFormattable
    {
        // Base32 "alphabet"
        private const string BASE32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        // Char to index lookup array for massive speedup since we can find a char's index in O(1). We use 255 as 'sentinel' value for invalid indexes.
        private static readonly byte[] C2B32 = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 255, 255, 255, 255, 255, 255, 255, 10, 11, 12, 13, 14, 15, 16, 17, 1, 18, 19, 1, 20, 21, 0, 22, 23, 24, 25, 26, 255, 27, 28, 29, 30, 31, 255, 255, 255, 255, 255, 255, 10, 11, 12, 13, 14, 15, 16, 17, 1, 18, 19, 1, 20, 21, 0, 22, 23, 24, 25, 26, 255, 27, 28, 29, 30, 31 };
        private static readonly int C2B32LEN = C2B32.Length;
        internal const long UNIXEPOCHMILLISECONDS = 62135596800000;

        // Internal parts of ULID
        private readonly byte _a; private readonly byte _b; private readonly byte _c; private readonly byte _d;
        private readonly byte _e; private readonly byte _f; private readonly byte _g; private readonly byte _h;
        private readonly byte _i; private readonly byte _j; private readonly byte _k; private readonly byte _l;
        private readonly byte _m; private readonly byte _n; private readonly byte _o; private readonly byte _p;

        // Default EPOCH used for Ulid's
        internal static readonly DateTimeOffset EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Represents the smallest possible value of <see cref="Ulid"/>. This field is read-only.
        /// </summary>
        public static readonly Ulid MinValue = new Ulid(EPOCH, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

        /// <summary>
        /// Represents the largest possible value of <see cref="Ulid"/>. This field is read-only.
        /// </summary>
        public static readonly Ulid MaxValue = new Ulid(DateTimeOffset.MaxValue, new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 });

        /// <summary>
        /// A read-only instance of the <see cref="Ulid"/> structure whose value is all zeros.
        /// </summary>
        public static readonly Ulid Empty = MinValue;

        /// <summary>
        /// Gets the "time part" of the <see cref="Ulid"/>.
        /// </summary>
        public DateTimeOffset Time => ByteArrayToDateTimeOffset(new[] { _a, _b, _c, _d, _e, _f });

        /// <summary>
        /// Gets the "random part" of the <see cref="Ulid"/>.
        /// </summary>
        public byte[] Random => new[] { _g, _h, _i, _j, _k, _l, _m, _n, _o, _p };

        /// <summary>
        /// Creates and returns a new <see cref="Ulid"/> based on the current (UTC) time and default
        /// (<see cref="CSUlidRng"/>) RNG.
        /// </summary>
        /// <returns>Returns a new <see cref="Ulid"/>.</returns>
        public static Ulid NewUlid() => NewUlid(DateTimeOffset.UtcNow, BaseUlidRng.DEFAULTRNG);

        /// <summary>
        /// Creates and returns a new <see cref="Ulid"/> based on the specified time and default
        /// (<see cref="CSUlidRng"/>) RNG.
        /// </summary>
        /// <param name="time">
        /// The <see cref="DateTimeOffset"/> to use for the time-part of the <see cref="Ulid"/>.
        /// </param>
        /// <returns>Returns a new <see cref="Ulid"/>.</returns>
        public static Ulid NewUlid(DateTimeOffset time) => NewUlid(time, BaseUlidRng.DEFAULTRNG);

        /// <summary>
        /// Creates and returns a new <see cref="Ulid"/> based on the current (UTC) time and using the specified RNG.
        /// </summary>
        /// <param name="rng">The <see cref="IUlidRng"/> to use for random number generation.</param>
        /// <returns>Returns a new <see cref="Ulid"/>.</returns>
        public static Ulid NewUlid(IUlidRng rng) => NewUlid(DateTimeOffset.UtcNow, rng);

        /// <summary>
        /// Creates and returns a new <see cref="Ulid"/> based on the specified time and using the specified RNG.
        /// </summary>
        /// <param name="time">
        /// The <see cref="DateTimeOffset"/> to use for the time-part of the <see cref="Ulid"/>.
        /// </param>
        /// <param name="rng">The <see cref="IUlidRng"/> to use for random number generation.</param>
        /// <returns>Returns a new <see cref="Ulid"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="rng"/> is <see langword="null"/>.</exception>
        public static Ulid NewUlid(DateTimeOffset time, IUlidRng rng)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));
            return new Ulid(time, rng.GetRandomBytes(time));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ulid"/> structure by using the specified array of bytes.
        /// </summary>
        /// <param name="bytes">
        /// A 16-element byte array containing values with which to initialize the <see cref="Ulid"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="bytes"/>  is anything but 16 bytes long.</exception>
        public Ulid(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if ( bytes.Length != 16)
                throw new ArgumentException("An array of 16 elements is required", nameof(bytes));

            _a = bytes[0];
            _b = bytes[1];
            _c = bytes[2];
            _d = bytes[3];
            _e = bytes[4];
            _f = bytes[5];
            _g = bytes[6];
            _h = bytes[7];
            _i = bytes[8];
            _j = bytes[9];
            _k = bytes[10];
            _l = bytes[11];
            _m = bytes[12];
            _n = bytes[13];
            _o = bytes[14];
            _p = bytes[15];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ulid"/> structure by using the specified <see cref="Guid"/>
        /// </summary>
        /// <param name="guid">A <see cref="Guid"/> representing a <see cref="Ulid"/>.</param>
        public Ulid(Guid guid) => this = new Ulid(guid.ToByteArray());

        /// <summary>
        /// Initializes a new instance of the <see cref="Ulid"/> structure by using the value represented by the
        /// specified string.
        /// </summary>
        /// <param name="ulid">A string that contains a <see cref="Ulid"/>.</param>
        public Ulid(string ulid) => this = Parse(ulid);

        // Internal constructor
        private Ulid(DateTimeOffset timePart, byte[] randomPart)
        {
            if (timePart < EPOCH)
                throw new ArgumentOutOfRangeException(nameof(timePart));
            if (randomPart.Length != 10)
                throw new InvalidOperationException($"{nameof(randomPart)} must be 10 bytes");

            var d = DateTimeOffsetToByteArray(timePart);
            _a = d[0]; _b = d[1]; _c = d[2]; _d = d[3]; _e = d[4]; _f = d[5];
            _g = randomPart[0]; _h = randomPart[1]; _i = randomPart[2]; _j = randomPart[3]; _k = randomPart[4];
            _l = randomPart[5]; _m = randomPart[6]; _n = randomPart[7]; _o = randomPart[8]; _p = randomPart[9];
        }

        #region Helper functions
        private static DateTimeOffset FromUnixTimeMilliseconds(long milliseconds)
        {
            var ticks = milliseconds * TimeSpan.TicksPerMillisecond + (UNIXEPOCHMILLISECONDS * 10000);
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        internal static long ToUnixTimeMilliseconds(DateTimeOffset value)
        {
            var milliseconds = value.Ticks / TimeSpan.TicksPerMillisecond;
            return milliseconds - UNIXEPOCHMILLISECONDS;
        }

        private static byte[] DateTimeOffsetToByteArray(DateTimeOffset value)
        {
            var mb = BitConverter.GetBytes(ToUnixTimeMilliseconds(value));
            return new[] { mb[5], mb[4], mb[3], mb[2], mb[1], mb[0] };                                  // Drop byte 6 & 7
        }

        private static DateTimeOffset ByteArrayToDateTimeOffset(byte[] value)
        {
            var tmp = new byte[] { value[5], value[4], value[3], value[2], value[1], value[0], 0, 0 };  // Pad with 2 "lost" bytes
            return FromUnixTimeMilliseconds(BitConverter.ToInt64(tmp, 0));
        }

        private static string ToBase32(byte[] value)
        {
            // Hand-optimized unrolled loops ahead
            switch (value.Length)
            {
                case 6:     // Time part
                    return new string(
                        new[] {
                        /* 0  */ BASE32[(value[0] & 224) >> 5],                             /* 1  */ BASE32[value[0] & 31],
                        /* 2  */ BASE32[(value[1] & 248) >> 3],                             /* 3  */ BASE32[((value[1] & 7) << 2) | ((value[2] & 192) >> 6)],
                        /* 4  */ BASE32[(value[2] & 62) >> 1],                              /* 5  */ BASE32[((value[2] & 1) << 4) | ((value[3] & 240) >> 4)],
                        /* 6  */ BASE32[((value[3] & 15) << 1) | ((value[4] & 128) >> 7)],  /* 7  */ BASE32[(value[4] & 124) >> 2],
                        /* 8  */ BASE32[((value[4] & 3) << 3) | ((value[5] & 224) >> 5)],   /* 9  */ BASE32[value[5] & 31],
                        }
                    );
                case 10:    // Random part
                    return new string(
                        new[] {
                        /* 0  */ BASE32[(value[0] & 248) >> 3],                             /* 1  */ BASE32[((value[0] & 7) << 2) | ((value[1] & 192) >> 6)],
                        /* 2  */ BASE32[(value[1] & 62) >> 1],                              /* 3  */ BASE32[((value[1] & 1) << 4) | ((value[2] & 240) >> 4)],
                        /* 4  */ BASE32[((value[2] & 15) << 1) | ((value[3] & 128) >> 7)],  /* 5  */ BASE32[(value[3] & 124) >> 2],  
                        /* 6  */ BASE32[((value[3] & 3) << 3) | ((value[4] & 224) >> 5)],   /* 7  */ BASE32[value[4] & 31],
                        /* 8  */ BASE32[(value[5] & 248) >> 3],                             /* 9  */ BASE32[((value[5] & 7) << 2) | ((value[6] & 192) >> 6)],
                        /* 10 */ BASE32[(value[6] & 62) >> 1],                              /* 11 */ BASE32[((value[6] & 1) << 4) | ((value[7] & 240) >> 4)],
                        /* 12 */ BASE32[((value[7] & 15) << 1) | ((value[8] & 128) >> 7)],  /* 13 */ BASE32[(value[8] & 124) >> 2],
                        /* 14 */ BASE32[((value[8] & 3) << 3) | ((value[9] & 224) >> 5)],   /* 15 */ BASE32[value[9] & 31],
                        }
                    );
            }
            throw new InvalidOperationException("Invalid length");
        }

        private static byte[] FromBase32(string v)
        {
            // Hand-optimized unrolled loops ahead
            unchecked
            {
                switch (v.Length)
                {
                    case 10:    // Time part
                        return new byte[]
                        {
                        /* 0 */ (byte)((C2B32[v[0]] << 5) | C2B32[v[1]]),                                   /* 1 */ (byte)((C2B32[v[2]] << 3) | (C2B32[v[3]] >> 2)),
                        /* 2 */ (byte)((C2B32[v[3]] << 6) | (C2B32[v[4]] << 1) | (C2B32[v[5]] >> 4)),       /* 3 */ (byte)((C2B32[v[5]] << 4) | (C2B32[v[6]] >> 1)),
                        /* 4 */ (byte)((C2B32[v[6]] << 7) | (C2B32[v[7]] << 2) | (C2B32[v[8]] >> 3)),       /* 5 */ (byte)((C2B32[v[8]] << 5) | C2B32[v[9]]),
                        };
                    case 16:    // Random part
                        return new byte[]
                        {
                        /* 0 */ (byte)((C2B32[v[0]] << 3) | (C2B32[v[1]] >> 2)),                            /* 1 */ (byte)((C2B32[v[1]] << 6) | (C2B32[v[2]] << 1) | (C2B32[v[3]] >> 4)),
                        /* 2 */ (byte)((C2B32[v[3]] << 4) | (C2B32[v[4]] >> 1)),                            /* 3 */ (byte)((C2B32[v[4]] << 7) | (C2B32[v[5]] << 2) | (C2B32[v[6]] >> 3)),
                        /* 4 */ (byte)((C2B32[v[6]] << 5) | C2B32[v[7]]),                                   /* 5 */ (byte)((C2B32[v[8]] << 3) | C2B32[v[9]] >> 2),
                        /* 6 */ (byte)((C2B32[v[9]] << 6) | (C2B32[v[10]] << 1) | (C2B32[v[11]] >> 4)),     /* 7 */ (byte)((C2B32[v[11]] << 4) | (C2B32[v[12]] >> 1)),
                        /* 8 */ (byte)((C2B32[v[12]] << 7) | (C2B32[v[13]] << 2) | (C2B32[v[14]] >> 3)),    /* 9 */ (byte)((C2B32[v[14]] << 5) | C2B32[v[15]]),
                        };
                }
            }
            throw new InvalidOperationException("Invalid length");
        }
        #endregion

        /// <summary>
        /// Converts the string representation of a <see cref="Ulid"/> equivalent.
        /// </summary>
        /// <param name="s">A string containing a <see cref="Ulid"/> to convert.</param>
        /// <returns>A <see cref="Ulid"/> equivalent to the value contained in s.</returns>
        /// <exception cref="ArgumentNullException">s is null or empty.</exception>
        /// <exception cref="FormatException">s is not in the correct format.</exception>
        public static Ulid Parse(string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException(nameof(s));

            var stripped = s.Replace("-", string.Empty);
            if (stripped.Length != 26)
                throw new FormatException("Invalid Base32 string");
            // Check if all chars are allowed by doing a lookup for each and seeing if we have an index < 32 for it
            for (var i = 0; i < 26; i++)
                if (stripped[i] >= C2B32LEN || C2B32[stripped[i]] > 31)
                    throw new FormatException("Invalid Base32 string");

            return new Ulid(ByteArrayToDateTimeOffset(FromBase32(stripped.Substring(0, 10))), FromBase32(stripped.Substring(10, 16)));
        }

        /// <summary>
        /// Converts the string representation of a <see cref="Ulid"/> to an instance of a <see cref="Ulid"/>. A return
        /// value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing the value to convert.</param>
        /// <param name="result">
        /// When this method returns, contains a <see cref="Ulid"/> equivalent of the <see cref="Ulid"/> contained in
        /// s, if the conversion succeeded, or <see cref="Empty"/> if the conversion failed. The conversion fails if
        /// the s parameter is null or <see cref="string.Empty"/>, is not of the correct format, or represents
        /// an invalid <see cref="Ulid"/> otherwise. This parameter is passed uninitialized; any value originally
        /// supplied in result will be overwritten.
        /// </param>
        /// <returns>true if s was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string s, out Ulid result)
        {
            try
            {
                result = Parse(s);
                return true;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                result = Empty;
                return false;
            }
        }

        /// <summary>
        /// Returns the <see cref="Ulid"/> in string-representation.
        /// </summary>
        /// <returns>The <see cref="Ulid"/> in string-representation.</returns>
        public override string ToString() => ToBase32(new[] { _a, _b, _c, _d, _e, _f })
                + ToBase32(new[] { _g, _h, _i, _j, _k, _l, _m, _n, _o, _p });

        /// <summary>
        /// Returns a 16-element byte array that contains the value of this instance.
        /// </summary>
        /// <returns>A 16-element byte array.</returns>
        public byte[] ToByteArray() => new byte[] { _a, _b, _c, _d, _e, _f, _g, _h, _i, _j, _k, _l, _m, _n, _o, _p };

        /// <summary>
        /// Returns a <see cref="Guid"/> that represents the value of this instance.
        /// </summary>
        /// <returns>A <see cref="Guid"/> that represents the value of this instance.</returns>
        public Guid ToGuid() => new Guid(ToByteArray());

        /// <summary>
        /// Returns a value indicating whether this instance and a specified <see cref="Ulid"/> object represent the
        /// same value.
        /// </summary>
        /// <param name="other">An <see cref="Ulid"/> to compare to this instance.</param>
        /// <returns>true if other is equal to this instance; otherwise, false.</returns>
        public bool Equals(Ulid other) => this == other;

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns>
        /// true if obj is a <see cref="Ulid"/> that has the same value as this instance; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            // Check that obj is a ulid first
            if (obj == null || !(obj is Ulid))
                return false;
            return Equals((Ulid)obj);
        }

        /// <summary>
        /// Compares this instance to a specified <see cref="Ulid"/> object and returns an indication of their relative
        /// values.
        /// </summary>
        /// <param name="other">A <see cref="Ulid"/> to compare to this instance.</param>
        /// <returns>
        ///     <para>
        ///     A signed number indicating the relative values of this instance and other.
        ///     </para>
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Return value</term>
        ///             <term>Description</term>
        ///         </listheader>
        ///         <item>
        ///             <term>A negative integer</term>
        ///             <term>This instance is less than other.</term>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <term>This instance is equal to other.</term>
        ///         </item>
        ///         <item>
        ///             <term>A positive integer</term>
        ///             <term>This instance is greater than other.</term>
        ///         </item>
        ///     </list>
        /// </returns>
        public int CompareTo(Ulid other)
        {
            var d = other.ToByteArray();

            if (_a != d[0]) return _a.CompareTo(d[0]); if (_b != d[1]) return _b.CompareTo(d[1]);
            if (_c != d[2]) return _c.CompareTo(d[2]); if (_d != d[3]) return _d.CompareTo(d[3]);
            if (_e != d[4]) return _e.CompareTo(d[4]); if (_f != d[5]) return _f.CompareTo(d[5]);
            if (_g != d[6]) return _g.CompareTo(d[6]); if (_h != d[7]) return _h.CompareTo(d[7]);
            if (_i != d[8]) return _i.CompareTo(d[8]); if (_j != d[9]) return _j.CompareTo(d[9]);
            if (_k != d[10]) return _k.CompareTo(d[10]); if (_l != d[11]) return _l.CompareTo(d[11]);
            if (_m != d[12]) return _m.CompareTo(d[12]); if (_n != d[13]) return _n.CompareTo(d[13]);
            if (_o != d[14]) return _o.CompareTo(d[14]); if (_p != d[15]) return _p.CompareTo(d[15]);

            return 0;
        }

        /// <summary>
        /// Compares this instance to a specified object and returns an indication of their relative values.
        /// </summary>
        /// <param name="obj">An object to compare, or null.</param>
        /// <returns>
        ///     <para>
        ///     A signed number indicating the relative values of this instance and value.
        ///     </para>
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Return value</term>
        ///             <term>Description</term>
        ///         </listheader>
        ///         <item>
        ///             <term>A negative integer</term>
        ///             <term>This instance is less than other.</term>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <term>This instance is equal to other.</term>
        ///         </item>
        ///         <item>
        ///             <term>A positive integer</term>
        ///             <term>This instance is greater than other.</term>
        ///         </item>
        ///     </list>
        /// </returns>
        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            if (!(obj is Ulid))
            {
                throw new ArgumentException("Object must be Ulid", nameof(obj));
            }
            return CompareTo((Ulid)obj);
        }

        /// <summary>
        /// Indicates whether the values of two specified <see cref="Ulid"/> objects are equal.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>true if x and y are equal; otherwise, false.</returns>
        public static bool operator ==(Ulid x, Ulid y)
        {
            var a = x.ToByteArray();
            var b = y.ToByteArray();

            for (var i = 0; i < 16; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        /// <summary>
        /// Indicates whether the values of two specified <see cref="Ulid"/> objects are not equal.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>true if x and y are not equal; otherwise, false.</returns>
        public static bool operator !=(Ulid x, Ulid y) => !(x == y);

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var d = ToByteArray();
                var hash = (int)2166136261;
                for (var i = 0; i < 16; i++)
                    hash = (hash * 16777619) ^ d[i];
                return hash;
            }
        }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about the <see cref="Ulid"/>.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the <see cref="Ulid"/>.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> argument is null.</exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("d", ToString(), typeof(string));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ulid"/> structure with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the <see cref="Ulid"/>.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> argument is null.</exception>
        /// <exception cref="SerializationException">The <see cref="Ulid"/> could not be deserialized correctly.</exception>
#pragma warning disable CA1801 // Parameter context of methor .ctor is never used
        private Ulid(SerializationInfo info, StreamingContext context)
#pragma warning restore CA1801 // Parameter context of methor .ctor is never used
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var d = Parse((string)info.GetValue("d", typeof(string))).ToByteArray();

            _a = d[0]; _b = d[1]; _c = d[2]; _d = d[3]; _e = d[4]; _f = d[5]; _g = d[6]; _h = d[7];
            _i = d[8]; _j = d[9]; _k = d[10]; _l = d[11]; _m = d[12]; _n = d[13]; _o = d[14]; _p = d[15];
        }


        /// <summary>
        /// Returns the <see cref="Ulid"/> in string-representation.
        /// </summary>
        /// <param name="format">Will be igored.</param>
        /// <param name="formatProvider">Will be igored.</param>
        /// <returns>The <see cref="Ulid"/> in string-representation.</returns>
        /// <remarks>Both the format and formatProvider are ignored since there is only 1 valid representation of a <see cref="Ulid"/>.</remarks>
        public string ToString(string format, IFormatProvider formatProvider) => ToString();

        /// <summary>
        /// Compares two <see cref="Ulid"/>s and returns <see langword="true"/>  when the <see cref="Ulid"/> on the
        /// left of the operator is less than the <see cref="Ulid"/> on the right of the operator, 
        /// <see langword="false"/> otherwise.
        /// </summary>
        /// <param name="left"><see cref="Ulid"/> on the left of the operator.</param>
        /// <param name="right"><see cref="Ulid"/> on the right of the operator.</param>
        /// <returns>
        /// Returns <see langword="true"/> when the <see cref="Ulid"/> on the left of the operator is less than the
        /// <see cref="Ulid"/> on the right of the operator, <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator <(Ulid left, Ulid right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Compares two <see cref="Ulid"/>s and returns <see langword="true"/>  when the <see cref="Ulid"/> on the
        /// left of the operator is less than, or equal to, the <see cref="Ulid"/> on the right of the operator, 
        /// <see langword="false"/> otherwise.
        /// </summary>
        /// <param name="left"><see cref="Ulid"/> on the left of the operator.</param>
        /// <param name="right"><see cref="Ulid"/> on the right of the operator.</param>
        /// <returns>
        /// Returns <see langword="true"/> when the <see cref="Ulid"/> on the left of the operator is less than,
        /// or equal to, the <see cref="Ulid"/> on the right of the operator, <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator <=(Ulid left, Ulid right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Compares two <see cref="Ulid"/>s and returns <see langword="true"/>  when the <see cref="Ulid"/> on the
        /// left of the operator is greater than the <see cref="Ulid"/> on the right of the operator, 
        /// <see langword="false"/> otherwise.
        /// </summary>
        /// <param name="left"><see cref="Ulid"/> on the left of the operator.</param>
        /// <param name="right"><see cref="Ulid"/> on the right of the operator.</param>
        /// <returns>
        /// Returns <see langword="true"/> when the <see cref="Ulid"/> on the left of the operator is greater than the
        /// <see cref="Ulid"/> on the right of the operator, <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator >(Ulid left, Ulid right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Compares two <see cref="Ulid"/>s and returns <see langword="true"/>  when the <see cref="Ulid"/> on the
        /// left of the operator is greater than, or equal to, the <see cref="Ulid"/> on the right of the operator, 
        /// </summary>
        /// <param name="left"><see cref="Ulid"/> on the left of the operator.</param>
        /// <param name="right"><see cref="Ulid"/> on the right of the operator.</param>
        /// <returns>
        /// Returns <see langword="true"/> when the <see cref="Ulid"/> on the left of the operator is greater than,
        /// or equal to, the <see cref="Ulid"/> on the right of the operator, <see langword="false"/> otherwise.
        /// </returns>
        public static bool operator >=(Ulid left, Ulid right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}