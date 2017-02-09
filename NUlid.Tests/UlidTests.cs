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
            CollectionAssert.AreEqual(FakeUlidRng.DEFAULTRESULT, target.Random);
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
            Assert.AreEqual(target.Time, StripMicroSeconds(DateTimeOffset.MaxValue));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Ulid_HandlesMaxTimePlus1MSCorrectly()
        {
            var maxtime_plusone = "76EZ91ZQ00";
            var target = new Ulid(maxtime_plusone + KNOWNMINRANDOM_STRING);
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
        public void Ulid_RNGs_WorkCorrectly()
        {
            // Generate 256 * 256 = 65536 random bytes; then find the distinct values; each byte should've been produced once (...usually)
            Assert.AreEqual(256, ((IUlidRng)new CSUlidRng()).GetRandomBytes(65536).Distinct().Count());
            Assert.AreEqual(256, ((IUlidRng)new SimpleUlidRng()).GetRandomBytes(65536).Distinct().Count());
        }

        [TestMethod]
        public void Ulid_TryParse_WorksCorrectly()
        {
            Ulid r1;
            Assert.IsFalse(Ulid.TryParse("X", out r1));
            Assert.AreEqual(r1, Ulid.Empty);

            Ulid r2;
            Assert.IsFalse(Ulid.TryParse(string.Empty, out r2));
            Assert.AreEqual(r2, Ulid.Empty);

            Ulid r3;
            Assert.IsFalse(Ulid.TryParse(null, out r3));
            Assert.AreEqual(r3, Ulid.Empty);

            Ulid r4;
            Assert.IsTrue(Ulid.TryParse(Ulid.MinValue.ToString(), out r4));
            Assert.IsTrue(Ulid.MinValue == r4);

            Ulid r5;
            Assert.IsTrue(Ulid.TryParse(Ulid.MaxValue.ToString(), out r5));
            Assert.IsTrue(Ulid.MaxValue == r5);

            Ulid r6;
            var target = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());
            Assert.IsTrue(Ulid.TryParse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING, out r6));
            Assert.AreEqual(target, r6);
        }

        [TestMethod]
        public void Ulid_Parse_WorksCorrectly()
        {
            Assert.AreEqual(Ulid.MinValue, Ulid.Parse(Ulid.MinValue.ToString()));
            Assert.AreEqual(Ulid.MaxValue, Ulid.Parse(Ulid.MaxValue.ToString()));

            var target = Ulid.NewUlid(KNOWNTIMESTAMP_DTO, new FakeUlidRng());
            Assert.AreEqual(target, Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING));
            Assert.AreEqual(target, new Ulid(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING));
        }

        [TestMethod]
        public void Ulid_IsCaseInsensitive()
        {
            var target = new Ulid(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING);

            Assert.AreEqual(target, new Ulid(KNOWNTIMESTAMP_STRING.ToLowerInvariant() + KNOWNRANDOMSEQ_STRING.ToLowerInvariant()));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ulid_ObjectCompareTo_Throws()
        {
            Ulid.NewUlid().CompareTo(new object());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ulid_Parse_ThrowsArgumentNullException_OnNull()
        {
            Ulid.Parse(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ulid_Parse_ThrowsArgumentNullException_OnEmptyString()
        {
            Ulid.Parse(string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Ulid_Parse_ThrowsFormatException_OnInvalidLengthString()
        {
            Ulid.Parse("TEST");
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Ulid_Parse_ThrowsFormatException_OnInvalidString1()
        {
            Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING.Replace('E', 'O')); // O is not in BASE32 alphabet
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void Ulid_Parse_ThrowsFormatException_OnInvalidString2()
        {
            Ulid.Parse(KNOWNTIMESTAMP_STRING + KNOWNRANDOMSEQ_STRING.Replace('E', '{')); // Test char after last index in C2B32 array
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ulid_Constructor_ThrowsArgumentException_OnInvalidByteArray()
        {
            new Ulid(new byte[] { 1, 2, 3 });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Ulid_NewUlid_ThrowsArgumentOutOfRangeException_OnTimestamp()
        {
            Ulid.NewUlid(Ulid.MinValue.Time.AddMilliseconds(-1));
        }

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

        [TestMethod]
        public void Ulid_IsSerializable_UsingBinaryFormatter()
        {
            var target = Ulid.NewUlid();

            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, target);
                stream.Position = 0;
                var result = formatter.Deserialize(stream);

                Assert.AreEqual(target, result);
            }
        }

        [TestMethod]
        public void Ulid_IsSerializable_UsingDataContract()
        {
            var target = Ulid.NewUlid();

            var serializer = new DataContractSerializer(target.GetType());
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, target);
                stream.Position = 0;
                var result = serializer.ReadObject(stream);

                Assert.AreEqual(target, result);
            }
        }
    }
}
