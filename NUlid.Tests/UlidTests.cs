using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NUlid.Tests
{
    [TestClass]
    public class UlidTests
    {

        //TODO: Test min/max timestamps (7ZZZZZZZZZ == ??)
        //                              (0000000000 == UNIX EPOCH)
        //                               76EZ91ZPZZ == Actual DateTimeOffset.MaxValue
        //                              and other edge-cases maybe
        //  and vice versa/overflows/whatever

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
            // We can't "simply" use DateTimeOffset.UtcNow since the resolution of a ulid is milliseconds, and UtcNow
            // has microseconds and more. So we drop that part by converting to UnixTimeMilliseconds and then back to
            // a DateTimeOffset.
            var time = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var target = Ulid.NewUlid(time);
            Assert.AreEqual(time, target.Time);
        }

        [TestMethod]
        public void Guid_CanConvertTo_Ulid()
        {
            var g = Guid.NewGuid();
            var u = new Ulid(g);

            Assert.AreEqual(g, u.ToGuid());
        }

        [TestMethod]
        public void Ulid_ToString_EncodesCorrectly()
        {
            var target = Ulid.NewUlid(DateTimeOffset.FromUnixTimeMilliseconds(1469918176385), new FakeUlidRng());

            Assert.AreEqual(26, target.ToString().Length);
            Assert.IsTrue(target.ToString().StartsWith("01ARYZ6S41"));
            Assert.IsTrue(target.ToString().EndsWith("DEADBEEFDEADBEEF"));
        }

        [TestMethod]
        public void Ulid_Empty_IsCorrectValue()
        {
            var target = Ulid.Empty;

            Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), target.Time);
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
            var a = Ulid.NewUlid(DateTimeOffset.FromUnixTimeMilliseconds(1469918176385), new FakeUlidRng());
            var b = Ulid.NewUlid(DateTimeOffset.FromUnixTimeMilliseconds(1469918176386), new FakeUlidRng());

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
            var a = Ulid.NewUlid(DateTimeOffset.FromUnixTimeMilliseconds(1469918176385), new FakeUlidRng());
            var b = Ulid.NewUlid(DateTimeOffset.FromUnixTimeMilliseconds(1469918176385), new FakeUlidRng());

            var c = Ulid.NewUlid(DateTimeOffset.FromUnixTimeMilliseconds(1469918176384), new FakeUlidRng());
            var d = Ulid.NewUlid(DateTimeOffset.FromUnixTimeMilliseconds(1469918176386), new FakeUlidRng());

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
        public void Ulid_ObjectCompareTo_WorksCorrectly()
        {
            var a = Ulid.NewUlid(DateTimeOffset.FromUnixTimeMilliseconds(1469918176385), new FakeUlidRng());
            var b = Ulid.NewUlid(DateTimeOffset.FromUnixTimeMilliseconds(1469918176385), new FakeUlidRng());

            var c = Ulid.NewUlid(DateTimeOffset.FromUnixTimeMilliseconds(1469918176384), new FakeUlidRng());
            var d = Ulid.NewUlid(DateTimeOffset.FromUnixTimeMilliseconds(1469918176386), new FakeUlidRng());

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
            var ts = DateTimeOffset.FromUnixTimeMilliseconds(1469918176385);
            var hashcodes = new List<int>()
            {
                Ulid.MinValue.GetHashCode(),
                Ulid.MaxValue.GetHashCode(),
                Ulid.NewUlid().GetHashCode(),
            };
            hashcodes.AddRange(Enumerable.Range(0, 1000).Select(i => Ulid.NewUlid(ts.AddMilliseconds(i)).GetHashCode()));
            hashcodes.AddRange(Enumerable.Range(0, 1000).Select(i => Ulid.NewUlid(ts.AddMilliseconds(i), rng).GetHashCode()));

            Assert.AreEqual(3 + 1000 + 1000, hashcodes.Distinct().Count());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ulid_ObjectCompareTo_Throws()
        {
            Ulid.NewUlid().CompareTo(new object());
        }
    }

}
