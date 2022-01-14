using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUlid.Rng;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NUlid.Tests
{
    [TestClass]
    public class UlidTests
    {
        // test-constants
        private static readonly DateTime UNIXEPOCH = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTimeOffset KNOWNTIMESTAMP_DTO = DateTimeOffset.FromUnixTimeMilliseconds(1469918176385);

        private const string KNOWNTIMESTAMP_STRING = "01ARYZ6S41";
        private const string KNOWNRANDOMSEQ_STRING = "DEADBEEFDEADBEEF";
        private const string KNOWNMAXTIMESTAMP_STRING = "76EZ91ZPZZ";
        private const string KNOWNMINRANDOM_STRING = "0000000000000000";
        private const string KNOWNMAXRANDOM_STRING = "ZZZZZZZZZZZZZZZZ";

        private static DateTimeOffset StripMicroSeconds(DateTimeOffset t) =>
            // We can't use DateTimeOffsets to compare since the resolution of a ulid is milliseconds, and DateTimeOffset
            // has microseconds and more. So we drop that part by converting to UnixTimeMilliseconds and then back to
            // a DateTimeOffset.
            DateTimeOffset.FromUnixTimeMilliseconds(t.ToUnixTimeMilliseconds());

        [TestMethod]
        public void NewUlid_Creates_NewUlid()
        {
            var target = Ulid.NewUlid();

            Assert.AreEqual(26, target.ToString().Length);
        }

        [TestMethod]
        public void NewUlid_Uses_SpecifiedRNG()
        {
            var target = Ulid.NewUlid(new FakeUlidRng());
            CollectionAssert.AreEqual(FakeUlidRng.DEFAULTRESULT.ToArray(), target.Random);
        }


        [TestMethod]
        public void NewUlid_Uses_SpecifiedTime()
        {
            var time = StripMicroSeconds(DateTimeOffset.UtcNow);
            var target = Ulid.NewUlid(time);
            Assert.AreEqual(time, target.Time);
        }

        [TestMethod]
        public void Guid_CanConvertTo_Ulid()
        {
            var g = Guid.NewGuid();
            var u = new Ulid(g);
            var t = new Ulid(Guid.Empty);

            Assert.AreEqual(g, u.ToGuid());
            Assert.AreEqual(Ulid.Empty, t);
            Assert.AreEqual(Guid.Empty, t.ToGuid());
        }

        [TestMethod]
        public void Ulid_ToString_EncodesCorrectly()
        {
            var target = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());

            Assert.AreEqual(26, target.ToString().Length);
            Assert.IsTrue(target.ToString().StartsWith(KNOWNTIMESTAMP_STRING));
            Assert.IsTrue(target.ToString().EndsWith(KNOWNRANDOMSEQ_STRING));
        }

        [TestMethod]
        public void Ulid_Empty_IsCorrectValue()
        {
            var target = Ulid.Empty;

            Assert.AreEqual(UNIXEPOCH, target.Time);
            Assert.IsTrue(target.Random.All(v => v == 0));
            Assert.AreEqual(new string('0', 26), target.ToString());
        }

        [TestMethod]
        public void Ulid_Parse_ParsesCorrectly()
        {
            var ulid = Ulid.NewUlid();
            var target = Ulid.Parse(ulid.ToString());

            Assert.IsTrue(target.Random.SequenceEqual(ulid.Random));
            Assert.AreEqual(ulid.Time, target.Time);
        }

        [TestMethod]
        public void Ulid_EqualsOperator_WorksCorrectly()
        {
            var a = Ulid.NewUlid();
            var b = new Ulid(a.ToByteArray());

            Assert.IsTrue(a == b);
        }

        [TestMethod]
        public void Ulid_NotEqualsOperator_WorksCorrectly()
        {
            var a = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());
            var b = Ulid.NewUlid(KNOWNTIMESTAMP_DTO.AddMilliseconds(1), new FakeUlidRng());

            Assert.IsTrue(a != b);
        }

        [TestMethod]
        public void Ulid_Equals_WorksCorrectly()
        {
            var a = Ulid.NewUlid();
            var b = new Ulid(a.ToByteArray());

            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a.Equals(a));
            Assert.IsFalse(a.Equals(Ulid.Empty));
        }

        [TestMethod]
        public void Ulid_ObjectEquals_WorksCorrectly()
        {
            var a = Ulid.NewUlid();
            var b = new Ulid(a.ToByteArray());

            Assert.IsTrue(a.Equals((object)b));
            Assert.IsTrue(a.Equals((object)a));
            Assert.IsFalse(a.Equals((object)Ulid.Empty));
            Assert.IsFalse(a.Equals(null));
            Assert.IsFalse(a.Equals(new object()));
        }

        [TestMethod]
        public void Ulid_CompareTo_WorksCorrectly()
        {
            var a = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());
            var b = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());

            var c = Ulid.NewUlid(KNOWNTIMESTAMP_DTO.AddMilliseconds(-1), new FakeUlidRng());
            var d = Ulid.NewUlid(KNOWNTIMESTAMP_DTO.AddMilliseconds(+1), new FakeUlidRng());

            Assert.AreEqual(0, a.CompareTo(b));
            Assert.AreEqual(1, a.CompareTo(c));
            Assert.AreEqual(-1, a.CompareTo(d));

            var rmin = a.ToByteArray(); rmin[15]--;
            var rplus = a.ToByteArray(); rplus[15]++;

            var e = new Ulid(rmin);
            var f = new Ulid(rplus);

            Assert.AreEqual(1, a.CompareTo(e));
            Assert.AreEqual(-1, a.CompareTo(f));
        }

        [TestMethod]
        public void Ulid_RandomIs_Immutable()
        {
            Ulid.MinValue.Random[0] = 42;
            Assert.AreEqual(0, Ulid.MinValue.Random[0]);

            Ulid.MaxValue.Random[0] = 42;
            Assert.AreEqual(255, Ulid.MaxValue.Random[0]);

            Ulid.Empty.Random[0] = 42;
            Assert.AreEqual(0, Ulid.Empty.Random[0]);

            var u = Ulid.NewUlid(new FakeUlidRng());
            u.Random[0] = 42;
            Assert.AreEqual(107, u.Random[0]);

            // Make sure when we pass an array into the constructor we cannot modify the source array (constructor copies, doesn't use reference)
            var x = Ulid.MaxValue.ToByteArray();
            var t = new Ulid(x);
            x[6] = 0;
            Assert.AreEqual(255, t.Random[0]);
        }

        [TestMethod]
        public void Ulid_HandlesMaxTimeCorrectly()
        {
            var target = new Ulid(KNOWNMAXTIMESTAMP_STRING + KNOWNMAXRANDOM_STRING);
            Assert.AreEqual(StripMicroSeconds(DateTimeOffset.MaxValue), target.Time);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Ulid_HandlesMaxTimePlus1MSCorrectly()
        {
            var maxtime_plusone = "76EZ91ZQ00";
            new Ulid(maxtime_plusone + KNOWNMINRANDOM_STRING);
        }



        [TestMethod]
        public void Ulid_ObjectCompareTo_WorksCorrectly()
        {
            var a = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());
            var b = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());

            var c = Ulid.NewUlid(KNOWNTIMESTAMP_DTO.AddMilliseconds(-1), new FakeUlidRng());
            var d = Ulid.NewUlid(KNOWNTIMESTAMP_DTO.AddMilliseconds(+1), new FakeUlidRng());

            Assert.AreEqual(0, a.CompareTo((object)b));
            Assert.AreEqual(1, a.CompareTo((object)c));
            Assert.AreEqual(-1, a.CompareTo((object)d));
            Assert.AreEqual(1, a.CompareTo(null));

            var rmin = a.ToByteArray(); rmin[15]--;
            var rplus = a.ToByteArray(); rplus[15]++;

            var e = new Ulid(rmin);
            var f = new Ulid(rplus);

            Assert.AreEqual(1, a.CompareTo((object)e));
            Assert.AreEqual(-1, a.CompareTo((object)f));
        }

        [TestMethod]
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

            Assert.AreEqual(3 + 1000 + 1000, hashcodes.Distinct().Count());
        }

        [TestMethod]
        public void Ulid_TryParse_WorksCorrectly()
        {
            Assert.IsFalse(Ulid.TryParse("X", out var r1));
            Assert.AreEqual(Ulid.Empty, r1);

            Assert.IsFalse(Ulid.TryParse(string.Empty, out var r2));
            Assert.AreEqual(Ulid.Empty, r2);

            Assert.IsFalse(Ulid.TryParse(null, out var r3));
            Assert.AreEqual(Ulid.Empty, r3);

            Assert.IsTrue(Ulid.TryParse(Ulid.MinValue.ToString(), out var r4));
            Assert.IsTrue(Ulid.MinValue == r4);

            Assert.IsTrue(Ulid.TryParse(Ulid.MaxValue.ToString(), out var r5));
            Assert.IsTrue(Ulid.MaxValue == r5);

            var target = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());
            Assert.IsTrue(Ulid.TryParse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING, out var r6));
            Assert.AreEqual(r6, target);
        }

        [TestMethod]
        public void Ulid_Parse_WorksCorrectly()
        {
            Assert.AreEqual(Ulid.MinValue, Ulid.Parse(Ulid.MinValue.ToString()));
            Assert.AreEqual(Ulid.MaxValue, Ulid.Parse(Ulid.MaxValue.ToString()));

            var target = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());
            Assert.AreEqual(Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING), target);
            Assert.AreEqual(new Ulid(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING), target);
        }

        [TestMethod]
        public void Ulid_IsCaseInsensitive()
        {
            var target = new Ulid(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING);

            Assert.AreEqual(new Ulid(KNOWNTIMESTAMP_STRING.ToLowerInvariant() + KNOWNRANDOMSEQ_STRING.ToLowerInvariant()), target);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ulid_ObjectCompareTo_Throws() => Ulid.NewUlid().CompareTo(new object());

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ulid_Parse_ThrowsArgumentNullException_OnNull() => Ulid.Parse(null);

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ulid_Parse_ThrowsArgumentNullException_OnEmptyString() => Ulid.Parse(string.Empty);

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Ulid_Parse_ThrowsFormatException_OnInvalidLengthString() => Ulid.Parse("TEST");

        [TestMethod]
        public void Ulid_Parse_Handles_IiLl_TreatedAs_One()  //https://www.crockford.com/base32.html
        {
            Assert.AreEqual(Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING), Ulid.Parse(KNOWNTIMESTAMP_STRING.Replace('1', 'i') + KNOWNRANDOMSEQ_STRING));
            Assert.AreEqual(Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING), Ulid.Parse(KNOWNTIMESTAMP_STRING.Replace('1', 'I') + KNOWNRANDOMSEQ_STRING));
            Assert.AreEqual(Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING), Ulid.Parse(KNOWNTIMESTAMP_STRING.Replace('1', 'l') + KNOWNRANDOMSEQ_STRING));
            Assert.AreEqual(Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING), Ulid.Parse(KNOWNTIMESTAMP_STRING.Replace('1', 'L') + KNOWNRANDOMSEQ_STRING));
        }

        [TestMethod]
        public void Ulid_Parse_Handles_Oo_TreatedAs_Zero()  //https://www.crockford.com/base32.html
        {
            Assert.AreEqual(Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING), Ulid.Parse(KNOWNTIMESTAMP_STRING.Replace('0', 'o') + KNOWNRANDOMSEQ_STRING));
            Assert.AreEqual(Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING), Ulid.Parse(KNOWNTIMESTAMP_STRING.Replace('0', 'O') + KNOWNRANDOMSEQ_STRING));
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Ulid_Parse_ThrowsFormatException_OnInvalidString1() => Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING.Replace('E', 'U')); // U is not in BASE32 alphabet

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Ulid_Parse_ThrowsFormatException_OnInvalidString2() => Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING.Replace('E', '{')); // Test char after last index in C2B32 array

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ulid_Constructor_ThrowsArgumentException_OnNullByteArray() => new Ulid((byte[])null);

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ulid_Constructor_ThrowsArgumentException_OnInvalidByteArray() => new Ulid(new byte[] { 1, 2, 3 });

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ulid_NewUlid_ThrowsArgumentNullException_OnNullRng() => Ulid.NewUlid(Ulid.MinValue.Time, null);

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Ulid_NewUlid_ThrowsArgumentOutOfRangeException_OnTimestamp() => Ulid.NewUlid(Ulid.MinValue.Time.AddMilliseconds(-1));

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Ulid_NewUlid_ThrowsInvalidOperationException_OnRNGReturningInsufficientBytes()
        {
            var rng = new FakeUlidRng(new byte[] { 1, 2, 3 });
            Ulid.NewUlid(rng);
        }

        [TestMethod]
        public void Ulid_TypeConverter_CanGetUsableConverter()
        {
            var converter = TypeDescriptor.GetConverter(typeof(Ulid));

            Assert.IsNotNull(converter);
            Assert.IsTrue(converter.CanConvertFrom(typeof(string)));
            Assert.IsTrue(converter.CanConvertFrom(typeof(byte[])));
        }

        [TestMethod]
        public void Ulid_TypeConverter_CanConvertFromString()
        {
            var original = Ulid.NewUlid();
            var ulidString = original.ToString();

            var converter = TypeDescriptor.GetConverter(typeof(Ulid));
            var converted = converter.ConvertFromString(ulidString);
            Assert.AreEqual(original, converted);
        }

        [TestMethod]
        public void Ulid_TypeConverter_CanConvertFromByteArray()
        {
            var original = Ulid.NewUlid();
            var ulidByteArray = original.ToByteArray();

            var converter = TypeDescriptor.GetConverter(typeof(Ulid));
            var converted = converter.ConvertFrom(ulidByteArray);
            Assert.AreEqual(original, converted);
        }

        [TestMethod]
        public void Ulid_TypeConverter_CanConvertToString()
        {
            var ulid = Ulid.NewUlid();
            var expectedUlidString = ulid.ToString();
            var converter = TypeDescriptor.GetConverter(typeof(Ulid));
            var ulidString = converter.ConvertToString(ulid);

            Assert.AreEqual(expectedUlidString, ulidString);
        }

        [TestMethod]
        public void Ulid_TypeConverter_CanConvertToByteArray()
        {
            var ulid = Ulid.NewUlid();
            var expectedByteArray = ulid.ToByteArray();

            var converter = TypeDescriptor.GetConverter(typeof(Ulid));
            var ulidByteArray = (byte[])converter.ConvertTo(ulid, typeof(byte[]));

            Assert.IsTrue(expectedByteArray.SequenceEqual(ulidByteArray));
        }

#pragma warning disable SYSLIB0011 // Type or member is obsolete
        [TestMethod]
        public void Ulid_IsSerializable_UsingBinaryFormatter()
        {
            var target = Ulid.NewUlid();

            var formatter = new BinaryFormatter();
            using var stream = new MemoryStream();
            formatter.Serialize(stream, target);
            stream.Position = 0;
            var result = formatter.Deserialize(stream);

            Assert.AreEqual(target, result);
        }

        [TestMethod]
        public void Ulid_IsSerializable_UsingDataContract()
        {
            var target = Ulid.NewUlid();

            var serializer = new DataContractSerializer(target.GetType());
            using var stream = new MemoryStream();
            serializer.WriteObject(stream, target);
            stream.Position = 0;
            var result = serializer.ReadObject(stream);

            Assert.AreEqual(target, result);
        }
#pragma warning restore SYSLIB0011 // Type or member is obsolete

        [TestMethod]
        public void InstanceCreatedWithoutRunningConstructor_Equals_EmptyUlid()
        {
            var target = Activator.CreateInstance<Ulid>();

            Assert.IsTrue(target.Equals(Ulid.Empty));
        }

        [TestMethod]
        public void MonotonicRng_Sequence_Testvectors()
        {
            var target = Ulid.Parse("01BX5ZZKBKACTAV9WEVGEMMVRY");

            var rng = new MonotonicUlidRng(new FakeUlidRng(), lastValue: target);

            Assert.AreEqual("01BX5ZZKBKACTAV9WEVGEMMVRZ", Ulid.NewUlid(target.Time, rng).ToString());
            Assert.AreEqual("01BX5ZZKBKACTAV9WEVGEMMVS0", Ulid.NewUlid(target.Time, rng).ToString());
            Assert.AreEqual("01BX5ZZKBKACTAV9WEVGEMMVS1", Ulid.NewUlid(target.Time, rng).ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(OverflowException))]
        public void MonotonicRng_Sequence_Throws_OnOverflow()
        {
            var target = Ulid.Parse("01BX5ZZKBKZZZZZZZZZZZZZZZX");

            var rng = new MonotonicUlidRng(new FakeUlidRng(), lastValue: target);

            Assert.IsTrue(Ulid.NewUlid(target.Time, rng).ToString().EndsWith("ZZZZZZZZZZZZZZZY"));
            Assert.IsTrue(Ulid.NewUlid(target.Time, rng).ToString().EndsWith("ZZZZZZZZZZZZZZZZ"));
            Ulid.NewUlid(target.Time, rng);  // Should throw
        }

        [TestMethod]
        public void MonotonicRng_Sequence_Resets_OnNewTimeStamp()
        {
            var target = Ulid.Parse("01BX5ZZKBKZZZZZZZZZZZZZZZX");

            var rng = new MonotonicUlidRng(new FakeUlidRng(), lastValue: target);

            Assert.IsTrue(Ulid.NewUlid(target.Time, rng).ToString().EndsWith("ZZZZZZZZZZZZZZZY"));
            Assert.IsTrue(Ulid.NewUlid(target.Time, rng).ToString().EndsWith("ZZZZZZZZZZZZZZZZ"));
            // Now we change the time, JUST in time before we overflow
            var result = Ulid.NewUlid(target.Time.Add(TimeSpan.FromMilliseconds(1)), rng);  // Should NOT throw
            Assert.AreEqual("01BX5ZZKBMDEADBEEFDEADBEEF", result.ToString()); // We should have a new "random" value and timestamp should have increased by one
        }

        [TestMethod]
        public void MicrosecondRng_Sequence_Testvectors()
        {
            var ts = new DateTimeOffset(2020, 2, 22, 0, 0, 0, TimeSpan.Zero);
            var target = Ulid.NewUlid(ts);
            var rng = new MicrosecondUlidRng(new FakeUlidRng());

            var microsecond = TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond / 1000);

            Assert.AreEqual("01E1N33600000DBEEFDEADBEEF", Ulid.NewUlid(target.Time.Add(microsecond * 0), rng).ToString());
            Assert.AreEqual("01E1N3360000MDBEEFDEADBEEF", Ulid.NewUlid(target.Time.Add(microsecond * 1), rng).ToString());
            Assert.AreEqual("01E1N33600018DBEEFDEADBEEF", Ulid.NewUlid(target.Time.Add(microsecond * 2), rng).ToString());
            Assert.AreEqual("01E1N3360001WDBEEFDEADBEEF", Ulid.NewUlid(target.Time.Add(microsecond * 3), rng).ToString());
        }

        [TestMethod]
        public void Parse_Allows_Hyphens()
        {
            Assert.AreEqual(Ulid.Parse("01BX5ZZKBKACTAV9WEVGEMMVRZ"), Ulid.Parse("01BX5ZZKBK-ACTA-V9WE-VGEM-MVRZ"));
            Assert.AreEqual(Ulid.Parse("01BX5ZZKBKACTAV9WEVGEMMVRZ"), Ulid.Parse("01BX5ZZKBK-ACTAV9WEVGEMMVRZ"));
        }

        [TestMethod]
        public void GreaterThanOperator()
        {
            Assert.IsTrue(new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS1") > new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS0"));
            Assert.IsFalse(new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS1") > new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS1"));
            Assert.IsFalse(new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS0") > new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS1"));
        }

        [TestMethod]
        public void GreaterThanOrEqualOperator()
        {
            Assert.IsTrue(new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS1") >= new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS0"));
            Assert.IsTrue(new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS1") >= new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS1"));
            Assert.IsFalse(new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS0") >= new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS1"));
        }


        [TestMethod]
        public void LessThanOperator()
        {
            Assert.IsTrue(new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS0") < new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS1"));
            Assert.IsFalse(new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS0") < new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS0"));
            Assert.IsFalse(new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS1") < new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS0"));
        }

        [TestMethod]
        public void LessThanOrEqualOperator()
        {
            Assert.IsTrue(new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS0") <= new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS1"));
            Assert.IsTrue(new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS0") <= new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS0"));
            Assert.IsFalse(new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS1") <= new Ulid("01BX5ZZKBKACTAV9WEVGEMMVS0"));
        }
    }
}