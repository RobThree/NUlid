using NUlid.Rng;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NUlid.Tests
{
    public class UlidTests
    {
        // test-constants
        private static readonly DateTime UNIXEPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTimeOffset KNOWNTIMESTAMP_DTO = DateTimeOffset.FromUnixTimeMilliseconds(1469918176385);

        private const string KNOWNTIMESTAMP_STRING = "01ARYZ6S41";
        private const string KNOWNRANDOMSEQ_STRING = "DEADBEEFDEADBEEF";
        private const string KNOWNMAXTIMESTAMP_STRING = "76EZ91ZPZZ";
        private const string KNOWNMINRANDOM_STRING = "0000000000000000";
        private const string KNOWNMAXRANDOM_STRING = "ZZZZZZZZZZZZZZZZ";

        private static DateTimeOffset StripMicroSeconds(DateTimeOffset t)
        {
            // We can't use DateTimeOffsets to compare since the resolution of a ulid is milliseconds, and DateTimeOffset
            // has microseconds and more. So we drop that part by converting to UnixTimeMilliseconds and then back to
            // a DateTimeOffset.
            return DateTimeOffset.FromUnixTimeMilliseconds(t.ToUnixTimeMilliseconds());
        }

        [Fact]
        public void NewUlid_Creates_NewUlid()
        {
            var target = Ulid.NewUlid();

            Assert.Equal(26, target.ToString().Length);
        }

        [Fact]
        public void NewUlid_Uses_SpecifiedRNG()
        {
            var target = Ulid.NewUlid(new FakeUlidRng());
            Assert.Equal(FakeUlidRng.DEFAULTRESULT, target.Random);
        }


        [Fact]
        public void NewUlid_Uses_SpecifiedTime()
        {
            var time = StripMicroSeconds(DateTimeOffset.UtcNow);
            var target = Ulid.NewUlid(time);
            Assert.Equal(time, target.Time);
        }

        [Fact]
        public void Guid_CanConvertTo_Ulid()
        {
            var g = Guid.NewGuid();
            var u = new Ulid(g);
            var t = new Ulid(Guid.Empty);

            Assert.Equal(g, u.ToGuid());
            Assert.Equal(Ulid.Empty, t);
            Assert.Equal(Guid.Empty, t.ToGuid());
        }

        [Fact]
        public void Ulid_ToString_EncodesCorrectly()
        {
            var target = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());

            Assert.Equal(26, target.ToString().Length);
            Assert.True(target.ToString().StartsWith(KNOWNTIMESTAMP_STRING));
            Assert.True(target.ToString().EndsWith(KNOWNRANDOMSEQ_STRING));
        }

        [Fact]
        public void Ulid_Empty_IsCorrectValue()
        {
            var target = Ulid.Empty;

            Assert.Equal(UNIXEPOCH, target.Time);
            Assert.True(target.Random.All(v => v == 0));
            Assert.Equal(new string('0', 26), target.ToString());
        }

        [Fact]
        public void Ulid_Parse_ParsesCorrectly()
        {
            var ulid = Ulid.NewUlid();
            var target = Ulid.Parse(ulid.ToString());

            Assert.True(target.Random.SequenceEqual(ulid.Random));
            Assert.Equal(ulid.Time, target.Time);
        }

        [Fact]
        public void Ulid_EqualsOperator_WorksCorrectly()
        {
            var a = Ulid.NewUlid();
            var b = new Ulid(a.ToByteArray());

            Assert.True(a == b);
        }

        [Fact]
        public void Ulid_NotEqualsOperator_WorksCorrectly()
        {
            var a = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());
            var b = Ulid.NewUlid(KNOWNTIMESTAMP_DTO.AddMilliseconds(1), new FakeUlidRng());

            Assert.True(a != b);
        }

        [Fact]
        public void Ulid_Equals_WorksCorrectly()
        {
            var a = Ulid.NewUlid();
            var b = new Ulid(a.ToByteArray());

            Assert.True(a.Equals(b));
            Assert.True(a.Equals(a));
            Assert.False(a.Equals(Ulid.Empty));
        }

        [Fact]
        public void Ulid_ObjectEquals_WorksCorrectly()
        {
            var a = Ulid.NewUlid();
            var b = new Ulid(a.ToByteArray());

            Assert.True(a.Equals((object)b));
            Assert.True(a.Equals((object)a));
            Assert.False(a.Equals((object)Ulid.Empty));
            Assert.False(a.Equals(null));
            Assert.False(a.Equals(new object()));
        }

        [Fact]
        public void Ulid_CompareTo_WorksCorrectly()
        {
            var a = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());
            var b = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());

            var c = Ulid.NewUlid(KNOWNTIMESTAMP_DTO.AddMilliseconds(-1), new FakeUlidRng());
            var d = Ulid.NewUlid(KNOWNTIMESTAMP_DTO.AddMilliseconds(+1), new FakeUlidRng());

            Assert.Equal(0, a.CompareTo(b));
            Assert.Equal(1, a.CompareTo(c));
            Assert.Equal(-1, a.CompareTo(d));

            var rmin = a.ToByteArray(); rmin[15]--;
            var rplus = a.ToByteArray(); rplus[15]++;

            var e = new Ulid(rmin);
            var f = new Ulid(rplus);

            Assert.Equal(1, a.CompareTo(e));
            Assert.Equal(-1, a.CompareTo(f));
        }

        [Fact]
        public void Ulid_RandomIs_Immutable()
        {
            Ulid.MinValue.Random[0] = 42;
            Assert.Equal(0, Ulid.MinValue.Random[0]);

            Ulid.MaxValue.Random[0] = 42;
            Assert.Equal(255, Ulid.MaxValue.Random[0]);

            Ulid.Empty.Random[0] = 42;
            Assert.Equal(0, Ulid.Empty.Random[0]);

            var u = Ulid.NewUlid(new FakeUlidRng());
            u.Random[0] = 42;
            Assert.Equal(107, u.Random[0]);

            // Make sure when we pass an array into the constructor we cannot modify the source array (constructor copies, doesn't use reference)
            var x = Ulid.MaxValue.ToByteArray();
            var t = new Ulid(x);
            x[6] = 0;
            Assert.Equal(255, t.Random[0]);
        }

        [Fact]
        public void Ulid_HandlesMaxTimeCorrectly()
        {
            var target = new Ulid(KNOWNMAXTIMESTAMP_STRING + KNOWNMAXRANDOM_STRING);
            Assert.Equal(target.Time, StripMicroSeconds(DateTimeOffset.MaxValue));
        }

        [Fact]
        public void Ulid_HandlesMaxTimePlus1MSCorrectly()
        {
            var maxtime_plusone = "76EZ91ZQ00";
            Assert.Throws<ArgumentOutOfRangeException>(() => new Ulid(maxtime_plusone + KNOWNMINRANDOM_STRING));
        }



        [Fact]
        public void Ulid_ObjectCompareTo_WorksCorrectly()
        {
            var a = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());
            var b = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());

            var c = Ulid.NewUlid(KNOWNTIMESTAMP_DTO.AddMilliseconds(-1), new FakeUlidRng());
            var d = Ulid.NewUlid(KNOWNTIMESTAMP_DTO.AddMilliseconds(+1), new FakeUlidRng());

            Assert.Equal(0, a.CompareTo((object)b));
            Assert.Equal(1, a.CompareTo((object)c));
            Assert.Equal(-1, a.CompareTo((object)d));
            Assert.Equal(1, a.CompareTo(null));

            var rmin = a.ToByteArray(); rmin[15]--;
            var rplus = a.ToByteArray(); rplus[15]++;

            var e = new Ulid(rmin);
            var f = new Ulid(rplus);

            Assert.Equal(1, a.CompareTo((object)e));
            Assert.Equal(-1, a.CompareTo((object)f));
        }

        [Fact]
        public void Ulid_GetHashCode_WorksCorrectly()
        {
            var rng = new FakeUlidRng();
            var hashcodes = new List<int>()
            {
                Ulid.MinValue.GetHashCode(),
                Ulid.MaxValue.GetHashCode(),
                Ulid.NewUlid().GetHashCode(),
            };
            hashcodes.AddRange(Enumerable.Range(0, 1000).Select(i => Ulid.NewUlid(KNOWNTIMESTAMP_DTO.AddMilliseconds(i)).GetHashCode()));
            hashcodes.AddRange(Enumerable.Range(0, 1000).Select(i => Ulid.NewUlid(KNOWNTIMESTAMP_DTO.AddMilliseconds(i), rng).GetHashCode()));

            Assert.Equal(3 + 1000 + 1000, hashcodes.Distinct().Count());
        }

        [Fact]
        public void Ulid_RNGs_WorkCorrectly()
        {
            // Generate 256 * 256 = 65536 random bytes; then find the distinct values; each byte should've been produced once (...usually)
            Assert.Equal(256, ((IUlidRng)new CSUlidRng()).GetRandomBytes(65536).Distinct().Count());
            Assert.Equal(256, ((IUlidRng)new SimpleUlidRng()).GetRandomBytes(65536).Distinct().Count());
        }

        [Fact]
        public void Ulid_TryParse_WorksCorrectly()
        {
            Ulid r1;
            Assert.False(Ulid.TryParse("X", out r1));
            Assert.Equal(r1, Ulid.Empty);

            Ulid r2;
            Assert.False(Ulid.TryParse(string.Empty, out r2));
            Assert.Equal(r2, Ulid.Empty);

            Ulid r3;
            Assert.False(Ulid.TryParse(null, out r3));
            Assert.Equal(r3, Ulid.Empty);

            Ulid r4;
            Assert.True(Ulid.TryParse(Ulid.MinValue.ToString(), out r4));
            Assert.True(Ulid.MinValue == r4);

            Ulid r5;
            Assert.True(Ulid.TryParse(Ulid.MaxValue.ToString(), out r5));
            Assert.True(Ulid.MaxValue == r5);

            Ulid r6;
            var target = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());
            Assert.True(Ulid.TryParse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING, out r6));
            Assert.Equal(target, r6);
        }

        [Fact]
        public void Ulid_Parse_WorksCorrectly()
        {
            Assert.Equal(Ulid.MinValue, Ulid.Parse(Ulid.MinValue.ToString()));
            Assert.Equal(Ulid.MaxValue, Ulid.Parse(Ulid.MaxValue.ToString()));

            var target = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());
            Assert.Equal(target, Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING));
            Assert.Equal(target, new Ulid(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING));
        }

        [Fact]
        public void Ulid_IsCaseInsensitive()
        {
            var target = new Ulid(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING);

            Assert.Equal(target, new Ulid(KNOWNTIMESTAMP_STRING.ToLowerInvariant() + KNOWNRANDOMSEQ_STRING.ToLowerInvariant()));
        }

        [Fact]
        public void Ulid_ObjectCompareTo_Throws()
        {
            Assert.Throws<ArgumentException>(() => Ulid.NewUlid().CompareTo(new object()));
        }

        [Fact]
        public void Ulid_Parse_ThrowsArgumentNullException_OnNull()
        {
            Assert.Throws<ArgumentNullException>(() => Ulid.Parse(null));
        }

        [Fact]
        public void Ulid_Parse_ThrowsArgumentNullException_OnEmptyString()
        {
            Assert.Throws<ArgumentNullException>(() => Ulid.Parse(string.Empty));
        }

        [Fact]
        public void Ulid_Parse_ThrowsFormatException_OnInvalidLengthString()
        {
            Assert.Throws<FormatException>(() => Ulid.Parse("TEST"));
        }

        [Fact]
        public void Ulid_Parse_ThrowsFormatException_OnInvalidString1()
        {
            Assert.Throws<FormatException>(() => Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING.Replace('E', 'O'))); // O is not in BASE32 alphabet
        }

        [Fact]
        public void Ulid_Parse_ThrowsFormatException_OnInvalidString2()
        {
            Assert.Throws<FormatException>(() => Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING.Replace('E', '{'))); // Test char after last index in C2B32 array
        }

        [Fact]
        public void Ulid_Constructor_ThrowsArgumentException_OnInvalidByteArray()
        {
            Assert.Throws<ArgumentException>(() => new Ulid(new byte[] { 1, 2, 3 }));
        }

        [Fact]
        public void Ulid_NewUlid_ThrowsArgumentOutOfRangeException_OnTimestamp()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Ulid.NewUlid(Ulid.MinValue.Time.AddMilliseconds(-1)));
        }

        [Fact]
        public void Ulid_NewUlid_ThrowsInvalidOperationException_OnRNGReturningInsufficientBytes()
        {
            var rng = new FakeUlidRng(new byte[] { 1, 2, 3 });
            Assert.Throws<InvalidOperationException>(() => Ulid.NewUlid(rng));
        }
    }
}
